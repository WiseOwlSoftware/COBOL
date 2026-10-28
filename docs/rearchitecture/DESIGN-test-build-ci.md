# DESIGN — Test, Build & CI Architecture (+ Migration Safety)

Status: DESIGN (rearchitecture dimension). Author: rearchitecture review. Date: 2026-07-07.
Scope: the harness/build/CI machinery that KEEPS THE BATTERY GREEN through the clean-slate rearchitecture of
`src/WiseOwl.Cobol.*`, plus the diagnostic-code registry, the characterization strategy, and the roadmap/DEVLOG
discipline that make each phase resumable and behavior-neutral.

This dimension underpins every other rearchitecture dimension: none of the god-class splits, pass-pipeline
extractions, or storage-form unifications can proceed safely unless the test net can PROVE, per phase, that
observable behavior did not change. The net once proved agreement with a legacy engine that was to be deleted — so it
was rearchitected first, to compare against committed goldens, and the engine is now deleted (P15 Cut 2).

---

## 1. Current state (as-built, grounded)

### 1.1 Test projects (four, two stacks)
| Project | Files | Role |
|---|---|---|
| `tests/WiseOwl.Cobol.Tests.Conformance` | ~80 `.cs` | ACTIVE greenfield net: differential feature tests (`*DifferentialTests.cs`), `NistDifferentialTests` (the corpus goldens, `[MemberData]` over `corpus.tsv` — see §3.2), `VersionMatrixTests`, `ConformanceSuiteTests`, `EditionHarness`, `CompilerUnderTest`/`CutRunner`. Those three theory-heavy families are **PARTITIONED across xUnit collections** — §3.11. |
| `tests/WiseOwl.Cobol.Tests.Unit` | ~17 `.cs` | Greenfield unit: runtime kernels, `ConstructRegistryDriftTests`, `ReservedWordsDriftTests`, `CheckOnlyCompileTests`, CLI parser, `EditionContextTests`. |

### 1.2 The differential oracle (the load-bearing risk)
`CompilerUnderTest.cs` defines `WiseOwlCobolCompiler` (drives `CompilerDriver`) and `CutRunner`. The ~60
`*DifferentialTests.cs` compile each source with WiseOwl COBOL and compare its normalized stdout
(`CutRunner.Normalize` = the guard's `normalize()`: drop CR, per-line trailing-trim) with a committed golden under
`tests/differential/` (`DifferentialGolden.Assert`, one mode). Those goldens were recorded once from the legacy
engine; the live legacy comparison, its bake and verify modes and the conformance project's `ProjectReference`s
to `Legacy.*` were removed by kb/Work PB2108 (R69), so a new golden is spec-derived and never baked.

`NistDifferentialTests` is DIFFERENT and safe: it compares WiseOwl COBOL output to committed goldens
(`tests/nist/valid/*.txt`), not to a live legacy run. Its program set and every chain's predecessors come from
`tests/nist/corpus.tsv` through `CorpusManifest`. The guard derives its population from the same file
(`scripts/guard-population.sh#guard_nist_population`: the asserted rows plus their declared chain predecessors), so
the two NIST legs run one population by construction (kb/Work PB925, PB3383); `CorpusManifestTests.GuardNistPopulation_*`
keep the derivation in place and the script's `--self-test` proves it.

### 1.3 Guard scripts (bash, Linux-only NIST loop, over the WiseOwl COBOL CLI)
⛔ **The NIST leg drives `cobol` (`src/WiseOwl.Cobol.Cli`), not the legacy CLI** — since kb/Work/PB750, which found
that the guards had hard-coded the legacy engine's `legacy.dll`. That binary's project
graph was the legacy engine plus `WiseOwl.Cobol.Frontend`; `WiseOwl.Cobol.Compiler` — the Roslyn code
generator that IS WiseOwl COBOL — is not in it, so the leg was structurally blind to every greenfield codegen defect
and each battery's headline `guard NIST: 353 MATCH` was a true statement about the ORACLE. Battery #58 is the
demonstration: `NC215A` printed a wrong answer (PB741) that `NistDifferentialTests_P0` caught and the guard's
353-MATCH/audit-CLEAN NIST line could not see.
⛔ **There is ONE guard and ONE runner of NIST programs** (kb/Work PB3383). A serial twin, `scripts/guard.sh`, ran
the population in ONE shared directory, in the order of its hand-written `NIST_TESTS` (kb/Work PB925), so a program
met every file the programs before it had left: once the keyed-store engine's writable OPEN fenced `tf024.txt`
(kb/Work PB3334), IX115A went red in the serial guard alone. It was retired with `guard-verify.sh`'s serial↔parallel
"equivalence proof", which was RELATIVE (two guards sharing a hole agreed: kb/Work PB473) where the audit below is
ABSOLUTE.
- `scripts/guard-compiler.sh` — ⭐ **the ONE place that answers "which compiler is this gate measuring?"**,
  sourced by the guard: `cobol` (src/WiseOwl.Cobol.Cli), the only compiler any gate drives since the legacy engine's
  retirement (kb/Work R69, PB2109). `guard_assert_compiler_identity` reads the resolved CLI's own `.deps.json` — the
  build's record of its project graph — and REFUSES to run when the closure does not contain `WiseOwl.Cobol.Compiler`,
  so a path typo, a stale bin dir or a project rename can never silently point the gate at another binary.
  `--self-test` proves the refusal fires (on synthetic manifests, so it needs no second compiler); `guard-verify.sh`
  runs it too.
- `scripts/guard-compile.sh` — the compile invocation, written once: `cobol` takes the test name as
  `--nist NAME` (its parser binds the next token, so `--nist prog.cob` would consume the SOURCE). Called by the
  guard's parallel compile and its serial re-observation retry.
- `scripts/guard-fast.sh` — THE NIST guard: builds the CLI under test, compiles its population (and, under a
  `GUARD_TESTS` subset, the subset's declared predecessors) through it, runs EVERY PROGRAM AS ITS OWN JOB across the
  cores (`JOBS=1` runs it serially) and diffs against `tests/nist/valid/` — every program, the ISO-rebaselined
  `divergent` goldens included, since those are exactly what the compiler must reproduce; a `TERMINATES` row must
  instead end naming its declared exception — then audits every verdict ABSOLUTELY against the manifest
  (`guard-nist-audit.sh`) and runs the golden-cleanliness sweep. Every summary line names the compiler:
  `=== NIST (cobol): … ===`. See §3.10's isolation row.
- `scripts/guard-run-group.sh` — the one runner: each named program in its own clean scratch dir after exactly its
  declared predecessors (NistRunner's rule); runs the per-test EVIDENCE RULES, which `guard-verdict.sh` holds, among
  them the written-files arm (§3.2.1).
- `scripts/guard-nist-audit.sh` — the population/manifest/expectation audit the guard runs.
- `scripts/guard-population.sh` — the ONE shell reader of the manifest: the population, the chain predecessors and the
  `TERMINATES` rows.
- `scripts/guard-verify.sh --self-test` — the guard's instrument self-test, discovered by `scripts/self_tests.py`
  (every gate and CI's audits run it) and run again by `guard-fast.sh` before it measures: the compiler-identity
  watchdog and the evidence-rule witnesses (§3.10), the run-directory witnesses among them.
- `scripts/version-continuity-sweep.sh` — INV-1: one warm `cobol check-batch` over ~350 programs × 4 editions
  (no Roslyn), fails on any `BREAKS`. THIS one drives the greenfield CLI.

**NIST is measured twice, by two different paths, and they are ONE POPULATION.** `NistDifferentialTests`
(6 partitions) drives `CompilerDriver` IN-PROCESS over the 364 green∪divergent rows on both OSes (kb/Work PB804 made the
15 programs that have a golden and that only the guard ran green rows too);
`guard-fast.sh` drives the `cobol` CLI as a separate process over the whole 376-program in-scope corpus from
bash on Linux. Neither subsumes the other — one covers the library API and Windows, the other covers the
shipped exe, the CCVS chain-isolation model and the compile+run health of the golden-less programs — and both
derive their population AND their expected verdict from `tests/nist/corpus.tsv`. `CorpusManifestTests` asserts
that structurally (guard population ⊇ every asserted program, ⊆ the manifest, surplus only `pending` rows, and
neither guard hard-coding a CLI path or naming the retired engine); `guard-nist-audit.sh` asserts it dynamically,
per program.

### 1.4 CI (`.github/workflows/build-and-test.yml`) — a gated matrix behind ONE required check
Twelve jobs. `changes` decides whether the matrix runs at all; `audits` runs on every push; then, concurrently, `guard` (WiseOwl COBOL parallel NIST,
ubuntu) · `greenfield-conformance` (ubuntu, 6 shards) · `greenfield-unit` (ubuntu) ·
`windows-conformance` (windows, the same 6 shards, Release) · `conformance-population` (the shards' union against
each platform's discovered population, `scripts/test_population.py`, §3.14.4) ·
`inv1-strong-2023` · `windows-build-test` (Release warnings-as-errors) · `windows-self-tests` (every script self-test
on a Windows checkout, no build, kb/Work PB3581); `dotnet-sdk-cache` (push to main only, both OSes: saves the pinned
SDK under main, below); and finally `ci-gate`. (The nightly
`legacy-oracle` job and the guard's legacy Unit and Integration legs were retired with the engine, kb/Work PB2108,
PB2109.) NuGet cached; `Generated/` regenerated per checkout (java+pwsh prerequisites).

**⛔ One pinned .NET SDK, installed one way, never through the aka.ms redirect (kb/Work PB3308;
`DotnetSdkInstallDriftTests`).** `scripts/install-dotnet-sdk.sh` pins the exact SDK version (one `global.json`'s
`10.0.100` and `rollForward: latestMinor` accept; the drift test checks it) and lists its feeds,
`builds.dotnet.microsoft.com` first and `ci.dot.net/public` second. It downloads `dotnet-sdk-<version>-<os>-<arch>` from
the first feed that serves it and its `.sha512`, checks the archive against that SHA-512, unpacks it, and only then writes
a completion marker. A transfer that breaks off is retried on the same feed, while a refusal (an HTTP error, a 404 or a
409) moves on to the next feed. Every building job gets its SDK from the composite action `.github/actions/setup-dotnet`:
it restores the SDK from the Actions cache keyed on the version, the OS and the architecture, runs the script (a no-op on
a cache hit), saves the SDK, puts it first on `PATH`, and fails unless `dotnet --version` reports the pinned version. A
merge group's run restores only what its own ref or main saved, and its ref is new every time, so `dotnet-sdk-cache`
saves the SDK under main on every push to main: a merge group then finds it even while every feed refuses. A warm-up
that fails warns and passes, because the merge group's own install still tries the feeds. `scripts/wsl/setup-wsl.sh`
calls the script. `scripts/cloud/setup-env.sh` runs before the repository is cloned, so it carries the script's install
block byte for byte between the `dotnet-sdk-install` markers. Before this, every building job ran `actions/setup-dotnet`
with `dotnet-version: 10.0.x`, which resolves "the newest 10.0" through aka.ms on every job, with no cache. A dropped
transfer cost one merge group (run 38974314534, 2026-10-16). From about 18:50Z on 2026-10-25 the redirect's storage
origin answered `409 PublicAccessNotPermitted`, so every code merge group failed `Setup .NET 10` while
builds.dotnet.microsoft.com served the same file. An exact version never goes through aka.ms. The pin is also kb/Work
PB1957 row 8's (a): CI and the local Linux hosts build with the same SDK patch. Moving to a newer SDK is one edit to
`DOTNET_SDK_VERSION`, copied into `setup-env.sh`.

**⛔ An artifact never decides a verdict (kb/Work PB3140; `CiArtifactDriftTests`).** Every test job uploads its
results (TRX, population listing, shard log, harness observation log) `if: ${{ always() }}`, so a red job's results
can be read without a re-run. Every upload goes through ONE step, the composite action `.github/actions/upload-results`:
it tries the upload three times (30 s, then 90 s apart; each retry `overwrite: true`), and when all three fail it warns
and passes, because a job's verdict is its tests'. The one artifact a verdict does depend on, a conformance shard's
trx and listing, is checked by the job that reads it: `conformance-population` downloads through
`.github/actions/download-results` (three attempts; only the third failure fails the step), and a shard whose upload
was lost is absent from the union it checks, so the guard fails there and names the missing cases. Before this, a bare
`actions/upload-artifact` step under `always()` made the artifact service part of the verdict: merge-group run
38502926616 (PR #39, 2026-10-14) passed 5338 of 5338 tests in a Windows conformance shard, its upload failed on
`ETIMEDOUT`, and the merge queue removed the pull request. No workflow names the raw upload or download action; the
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

**The skip moved one layer down, and is now the inert lane (§3.18).** `paths-ignore` used to make the workflow decline
to START for a doc-only push — incompatible with a per-commit required check, which has nothing to satisfy when no run
exists. `changes` now decides it, by calling `scripts/inert_lane.py classify` over `base .. github.sha` (§3.18: the one
classification, which the local gate and the Linux gate call too), and outputs `run_matrix`; every other job carries
`needs: changes` + `if: needs.changes.outputs.run_matrix == 'true'`. A merge group's base is its
`merge_group.base_sha`, so a group of inert pull requests skips the matrix; a pull request never
runs it (its run is the cheap jobs, §3.17.6); a push to main diffs from `before`, and with no resolvable `before` the
matrix runs. `changes` also short-circuits a push whose sha already carries a green run of the workflow from an event
other than `pull_request` — the merge group's, since the queue advances main to the very commit it tested — so main's
push run does not pay for the matrix a second time, and it carries the drift check that asserts every job id is in
`ci-gate`'s `needs` list.

**The landing protocol is `enqueue.py land` into GitHub's merge queue (§3.17):** main takes only pull requests the queue
tested (the ruleset binds administrators), so the push trigger is `branches: [ main ]` and the one push it sees is the
merge-queue commit. `cancel-in-progress` is restricted to `pull_request`, because a cancelled push run leaves its sha
permanently unlandable. The direct-push landing script that preceded the queue, its `ci/` landing branches and its
landing lease are deleted (kb/Work PB3024's closing change).

### 1.5 Drift discipline (a genuine strength — preserve it)
`ConstructRegistryDriftTests` binds `constructs.json` ↔ in-code `ConstructRegistry` both directions;
`conformance-suite/harness/generate.py --check` refuses every on-disk file no test declares (it replaced the corpus's
`Manifest_CoversEveryProgram_NoOverlap`, kb/Work PB3177);
`ReservedWordsDriftTests` binds `reserved-words.json`. Nothing silently undiscovered.

### 1.6 Diagnostics
161 distinct `COBOL####` codes exist as **bare string literals**; only 4 are named consts (`EditionCodes.cs`).
`COBOL0899` (catch-all "recognized-not-implemented") appears at ~47 sites; several codes are reused for
unrelated rules (e.g. `1533` ×3). There is no registry, no `docs/DIAGNOSTICS.md`, no per-code metadata, so tests
and `--suppress` cannot target a specific rule and the version matrix cannot enumerate diagnostics.

### 1.7 The fleet build guard (`scripts/hooks/fleet_active_build.py`) — a PreToolUse gate on `dotnet *`
Wired in `.claude/settings.json` for both `Bash(dotnet *)` and `PowerShell(dotnet *)`. It **denies**
`dotnet build|test|clean|publish|run|msbuild` while a live subagent is probing the binaries the build would
replace — the guard that exists because ~6 rebuilds under a running 60-agent fleet made all 60 verdicts unusable
(2026-08-04, PB15). Liveness is transcript MTIME within 120 s, scoped to this session's id.
**The unit of the freeze is the WORKING TREE, not the session (2026-09-01).** It denies iff some *foreign* live
agent works in the caller's own tree, and both trees are derived, never listed: the caller's tree is the nearest
ancestor holding a `.git` entry (a directory in the main checkout, a file in a worktree) of WHERE THE BUILD RUNS —
the project a `dotnet` command names, else the directory the shell is in when it runs it. That directory comes from
`scripts/hooks/shell_location.py`, the ONE parser of both shells' command lines and location changes (`cd`,
`pushd`/`popd`, a subshell, an and-or list a single `&` backgrounds, `bash -c`; `Set-Location`/`sl`/`chdir`/
`Push-Location` with `-Path`/`-LiteralPath`, `pwsh -WorkingDirectory`, `Start-Process -WorkingDirectory`, cmd.exe's
`cd /d`), which `worktree_rm_allow.py`, `forbidden_commands.py` (every rule; kb/Work PB3095, PB3198) and
`status_guard.py` share (kb/Work PB2599; each used to carry its own partial parser, and a PowerShell `Set-Location
<worktree>; dotnet build` was judged to build in the main checkout). It is the ONE model of a command line those rules
read (kb/Work PB3198): heredoc bodies cut out at their own delimiter lines, and every place a shell takes a command line
from walked as one (a `$( … )`, backquote or PowerShell expression or script block; `bash -c`, `pwsh -Command` /
`-EncodedCommand`, `cmd /c`, `eval`, `Invoke-Expression`, also behind `timeout`/`env`/`xargs`/`wsl -e`; a heredoc,
here-string or literal pipe a shell reads as its script), each step marked with how many substitutions enclose it, so
the steps of one level are that level's own command line (a pipe inside `$( … )` reads the stage beside it), and a
walk carrying what its own stdin holds: the read side of a `|` reads the stage before it (a group's last step
when the group writes the pipe), a group reading a pipe, a Bash substitution and a `-c` payload hand the stdin they
inherit to their commands, and only a `|` of a walk's OWN makes a command a pipeline side (a `cd` inside a group
that reads a pipe moves the group's later steps);
a foreign agent's tree is the tree of the directory **the agent was launched in**, which its own transcript records
on its first record (`cwd`; `launch_cwd`, `agent_place`; kb/Work PB3770). The host writes that field however it
starts an agent, so one mechanism covers `Agent(isolation="worktree")`'s `agent-<id>`, a rolling wave's
`wf_<run>-<n>`, a named `git worktree add` and the main checkout (the guard used to infer the tree from the existence of
`<main>/.claude/worktrees/agent-<agentId>` and read every other agent as a main-checkout agent). So N implementer
agents in N worktrees build in parallel, a main-tree build is still denied by a live main-tree agent, and a main-tree
build is *allowed* while only worktree agents are live. A build outside every working tree (a read-only adjudicator's
scratch probe) writes next to the projects of its graph — the project or solution it names, or the only one in the
directory, and the closure of their `ProjectReference`s, solution members and a file-based app's `#:project`
directives (`build_places`) — so it is denied only by an agent working in a tree such a project references.
Fail-open on any error; an **unknown** answer reverts to the old session-wide deny rather than to an allow: a
directory the command line does not determine, an agent whose transcript names no launch directory or whose launch
directory no longer exists, and a scratch graph the guard cannot read to the end (a missing project, a `$(…)` in a
reference, an `<Import>`, a moved output path, a `Directory.Build.props`/`.targets` in an ancestor directory).
`--self-test` fires every branch over real temporary worktrees, scratch projects and agent transcripts, and is the only
thing that proves the ALLOW arm — three of this hook's four defects were it failing closed.

---

## 2. Problems this design must fix

1. **The net evaporates at G8.** ~60 differential tests are oracle-coupled to code that gets deleted.
2. **No behavior-neutrality proof for a refactor.** A rearchitecture phase that splits `StatementBinder` or unifies
   `StoreAsImage` has NO characterization gate: it relies on output goldens for the subset of programs that happen
   to be in the corpus, and nothing snapshots the generated C# to catch an unintended emit change.
3. **"Which programs are green" has one source for the tests and a second, hand-written one for the guards.**
   `tests/nist/corpus.tsv` replaced the `[InlineData]` list and the chain table at P0, and the guard's population
   (`guard_nist_population`) since kb/Work PB925 and PB3383 retired the serial guard's hand-written `NIST_TESTS`.
4. ~~**The authoritative NIST gate tests frozen code.**~~ **CLOSED 2026-09-06 (kb/Work/PB750).** CI's heaviest job
   (`guard`) drove the legacy engine for the whole of the rearchitecture, so the compiler under development was
   gated on NIST only by the in-process differential suite — and battery #58's `NC215A` wrong answer proved the
   cost. Both guards now resolve their compiler through `scripts/guard-compiler.sh` (`cobol`, the only compiler
   since kb/Work PB2109), refuse to start unless the resolved binary's `.deps.json` closure contains the code
   generator, and name the compiler in every verdict line.
5. **NIST loop is Linux-only bash**, so the Windows job cannot run it; the authoritative regression has an OS gap.
6. **Diagnostics are unaddressable.** No registry ⇒ the version matrix, `--suppress`, and per-rule tests cannot key
   on a stable id; a renumber/reuse is invisible.
7. **`CheckOnly` runs full Emit** (no Bind phase boundary) ⇒ the fast verdict path is not actually fast, and a
   characterization harness cannot snapshot the bound tree independently of emission.
8. **Roslyn reference set uncached** ⇒ the in-process battery pays ~180 `MetadataReference.CreateFromFile` per
   compile (thousands of times per run).
9. **Roadmap SSOT was a 100 KB `resume-prompt.md`** (since absorbed into `docs/REARCHITECTURE_PLAN.md` and deleted) — excellent for session resume, but there is no compact,
   phase-structured, exit-criteria-bearing rearchitecture roadmap doc that a future engineer resumes the
   MIGRATION (not the feature drive) from.

---

## 3. Target design

### 3.0 Guiding principle: three neutrality gates, ranked
A rearchitecture phase is "behavior-neutral" iff, in order of authority:
1. **Output goldens** (primary, spec-anchored): NIST `valid/*.txt` + corpus `.out` + negative `.err`. These are the
   ISO-conforming oracle and MUST stay byte-identical across every refactor phase. Non-negotiable.
2. **Diagnostic goldens** (primary): every emitted diagnostic (code + message + location) for a fixed corpus of
   positive and negative programs is snapshotted; a refactor must not change the diagnostic surface.
3. **Emitted-C# snapshots** (secondary, advisory): the generated `.g.cs` for a representative program set is
   snapshotted. A refactor that is a pure reorg SHOULD leave these byte-identical; a refactor that intentionally
   changes emission form (e.g. structured `Place` → different but equivalent C#) RE-BASELINES the snapshot **with
   explicit review**, and gate (1) proves the re-baseline is behavior-preserving. The runtime version a module
   records (`CobolRepository`, `EnsureRegistered`) is the package version, which every runtime major bump raises
   (DESIGN-external-repository.md §15.4): the snapshot holds it as `{RUNTIME-VERSION}`, substituted only where it is
   the runtime's own version, so a bump re-baselines nothing (a version reaches no test through code impact, so
   leg 1 never planned these cases on one) and an emitter stamping any other version still diffs.

Gates (1) and (2) are hard (red = stop). Gate (3) is a reviewed diff, never auto-accepted, never silently drifting.

### 3.1 Test project taxonomy (target)
Collapse to a coherent, layered set under one namespace root `WiseOwl.Cobol.Tests.*`:

| Project | Replaces | Contents |
|---|---|---|
| `tests/WiseOwl.Cobol.Tests.Unit` | (kept) | Pure unit: runtime kernels, binder/emitter component units, CLI/driver, drift tests. Fast, no Roslyn where avoidable. |
| `tests/WiseOwl.Cobol.Tests.Conformance` | (kept, refocused) | End-to-end program behavior: NIST goldens, per-edition corpus, version matrix, negative corpus. Compares to COMMITTED goldens only — no live second compiler. |
| `tests/WiseOwl.Cobol.Tests.Characterization` | NEW | The behavior-neutrality harness: diagnostic snapshots + emitted-C# snapshots (gates 2 & 3) over a fixed "characterization corpus". Runs on every phase. |

Rationale: `ConformanceSuiteTests` is the one suite runner (it replaced `CorpusRunnerTests`, kb/Work PB3177); the legacy `ConformanceTests` runner and its `GreenfieldOnly`/`LegacyDivergent` skip sets were deleted with the legacy test projects (PB2110).

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
The behavior-neutrality proof the rearchitecture needs. Structure:

```
tests/WiseOwl.Cobol.Tests.Characterization/
  CharacterizationCorpus.cs      # discovers tests/characterization/**/*.cob (+ per-file .std directive)
  DiagnosticSnapshotTests.cs     # gate (2)
  EmittedCSharpSnapshotTests.cs  # gate (3)
  Snapshots/                     # committed golden snapshots (.diag.txt, .g.cs.txt)
tests/characterization/
  positive/*.cob                 # programs that compile clean — snapshot their emitted C#
  negative/*.cob                 # programs that must diagnose — snapshot their diagnostic list
  README.md
```

Signatures (concrete):

```csharp
public interface ICompilerProbe {
    // Bind-only: no Roslyn. Returns the diagnostic list (code+message+location) and, optionally, the emitted C#.
    ProbeResult Probe(string source, int edition, bool emit);
}
public sealed record ProbeResult(
    bool Ok,
    IReadOnlyList<Diagnostic> Diagnostics,   // structured — see 3.5
    string? EmittedCSharp);                   // null unless emit:true
```

- `DiagnosticSnapshotTests` compiles each `characterization/**` program, formats its diagnostics deterministically
  (`{code}\t{severity}\t{line}:{col}\t{message}`), and asserts equality with `Snapshots/<name>.diag.txt`.
- `EmittedCSharpSnapshotTests` emits the C# for each `positive/*.cob` and asserts equality with
  `Snapshots/<name>.g.cs.txt` (line-ending normalized). Roslyn is NOT invoked — this snapshots the emitter output
  string, which is cheap and deterministic.
- Re-baselining: `WISEOWL_COBOL_UPDATE_SNAPSHOTS=1 dotnet test …/Characterization` rewrites the `Snapshots/` files. This
  is a DELIBERATE, reviewed action (the diff shows in the PR). Gate (1) — output goldens — proves any re-baseline
  is behavior-preserving. Never run in CI; CI always compares.

The characterization corpus is seeded ONCE, at the START of the rearchitecture, from the current (pre-refactor)
emitter, so the first snapshot is "how the emitter emits today." Every subsequent phase must match it (or
review-re-baseline). This is the missing "prove I changed nothing" gate.

### 3.4 Oracle bake-out: converting the differential net to goldens (the G8-survival migration)
The ~60 `*DifferentialTests.cs` currently assert `wiseowl-cobol == legacy` at RUN time. Before the legacy engine is
deleted, run a ONE-TIME bake that freezes the legacy output as a committed golden, then rewrite each test to assert
`wiseowl-cobol == golden`.

Mechanism:
1. Add `CutRunner`-driven `DifferentialBakeTool` (a `[Fact(Skip=…)]`-gated maintenance test, or a small
   console tool in `tools/`) that, for every differential test's source, runs `LegacyCompiler`, captures normalized
   stdout, and writes `tests/differential/<TestClass>/<case>.out`.
2. Rewrite the differential base to a golden-comparison base:
   ```csharp
   protected void AssertMatchesGolden(string source, string goldenName, int edition = 85)
       => Assert.Equal(ReadGolden(goldenName), Cn.CompileAndRun(source).stdout);
   ```
   The legacy `ICompilerUnderTest`/`LegacyCompiler` and the `WiseOwl.Cobol==Legacy` asserts are deleted.
3. A `divergent`-style escape hatch stays for the handful of legacy-non-conforming cases (already tracked in
   `LegacyDivergent`) — those goldens are hand-authored to the ISO value.

After the bake, the entire greenfield net is self-standing: it depends on committed goldens, not on the legacy
`ProjectReference`. The legacy test projects can be deleted at G8 with zero net loss.
⛔ **The CLI-level NIST guard (`guard-fast.sh`) cannot** — not any more (kb/Work/PB750). It no longer drives the
legacy engine: it is the CLI-level, separate-process NIST leg over `cobol`, and deleting it would drop the
golden-less chain predecessors' compile+run health, the CCVS chain-isolation model and the shipped exe's own
path. What retired with the engine (kb/Work PB2109) was the legacy arm of `guard-compiler.sh` and the legacy
exemption list, not the leg.
**This bake is a PREREQUISITE gate for G8 and for retiring the legacy CI job.**

### 3.5 Diagnostic-code registry (`WiseOwl.Cobol.Diagnostics`)
A first-class subsystem (also required by the Editions dimension). Minimal shape:

```csharp
public sealed record DiagnosticDescriptor(
    string Code,           // "COBOL1533" — one code, one rule
    Severity DefaultSeverity,
    string SpecRef,        // "ISO §8.4.3.9.4" — the citation
    string MessageTemplate,// "strong TYPE mismatch: {0} vs {1}"
    string? SuppressKey);  // stable --suppress token

public static class Diag {                    // the catalogue (generated-friendly consts)
    public static readonly DiagnosticDescriptor StrongTypeMoveMismatch = new("COBOL1533", …);
    // … one entry per distinct rule …
}
public readonly record struct Diagnostic(
    DiagnosticDescriptor Descriptor, string Message, SourceSpan Location, Severity Severity);
```

- `EditionContext.Error("COBOL####", text)` → `sink.Report(Diag.X, args…)`. The 47 `0899` sites split into
  named unimplemented-feature descriptors behind a tracked list; `1533`-style reuse is un-merged into distinct
  codes.
- `DiagnosticRegistryDriftTests` (in `Tests.Unit`): every `Diag.*` descriptor has a unique code; every code emitted
  anywhere in the compiler is a registered descriptor (a source-scan or a runtime-collected set). Mirrors the
  existing `ConstructRegistryDriftTests` discipline.
- A build step (or a `[Fact]` that writes when `WISEOWL_COBOL_UPDATE_DOCS=1`) generates `docs/DIAGNOSTICS.md` from the
  catalogue — the single human-readable code table understandability #1 asks for.

This makes gate (2) precise (snapshots key on stable codes) and lets the version matrix and `--suppress` target a
rule.

**The wire encoding of a diagnostic is part of the diagnostic** (`kb/Work/PB899`). A message carries its ISO
citation, so it carries non-ASCII — `(ISO §11.10.3 SR5–6)` holds U+00A7 and U+2013. `WiseOwl.Cobol.Runtime.IO.StandardStreams.EnsureUtf8()`
is the ONE place that rule is written down, and BOTH process entries call it: `Program.Main` in the CLI before it
parses an argument, and `ProgramTable.RunModule` for a compiled program's `Main` (never a host entry such as
`RunMain`, whose host owns its console: kb/Work PB3193). The run-unit entry is where it used to live ALONE, so every
citation the compiler printed left the process at the OS code page (on cp437 the section sign is the single byte
`0x15`) in every redirected capture and every CI log. No golden could see it: the corpus runner matches `.err`
files on a pure-ASCII diagnostic CODE substring, and the harness decodes every capture as UTF-8 (and encodes what it
pipes to standard input as UTF-8, the ACCEPT device's decoding, kb/Work PB3193). The witness is
therefore a BYTE assertion through a redirected stream, `CliDiagnosticEncodingTests`, which drives the CLI from a
console forced to cp437 on Windows and FAILS if that code page was not actually in force — a child with no console
inherits .NET's UTF-8 default and would pass on the broken build.

### 3.6 Build & driver seams the harness depends on
Two build-side changes this dimension REQUIRES (owned by the driver/emitter dimensions but gated here):
- **Real Bind phase boundary.** Extract the binder passes — reached today through the `CSharpEmitter.Bind` host
  facade — into a standalone `Binding.BindPipeline.Bind(tree, edition) → BoundCompilation`. Then: `CheckOnly`/`--check-batch` stop after Bind
  (fast verdict, no Emit); `ICompilerProbe.Probe(emit:false)` returns diagnostics from Bind only; the
  characterization emitter snapshot is a pure `Emit(BoundCompilation)`.
- **Cache the Roslyn reference set.** `RoslynBackend.ReferenceAssemblies()` →
  `static readonly Lazy<ImmutableArray<MetadataReference>>`. Single highest-leverage battery-throughput fix.

### 3.7 Guard consolidation (cross-platform, greenfield-first)
Target: the authoritative regression is the in-process `dotnet test` battery (runs on every OS), NOT a Linux bash
loop over frozen code. ⚠ **"over frozen code" was the whole objection and it no longer applies** (PB750): the
bash loop drives `cobol`. It stays a SECOND path — the shipped exe, out of process — deliberately, and the two
are reconciled by `corpus.tsv` + `CorpusManifestTests` + `guard-nist-audit.sh` rather than by one replacing the
other. Scripts collapse to:

- KEEP `scripts/version-continuity-sweep.sh` (the check-batch INV-1 sweep — it drives the greenfield CLI and is
  already fast/portable) but add a `.ps1` sibling.
- ⛔ **REVISED by kb/Work/PB750:** `guard-fast.sh`, `guard-run-group.sh`, `guard-verify.sh`, `guard-compiler.sh`,
  `guard-compile.sh`, `guard-verdict.sh` and `guard-nist-audit.sh` no longer "exist to parallelize the legacy
  NIST loop" — they ARE the CLI-level WiseOwl COBOL NIST leg, and they carry the verdict-evidence rules, the chain
  isolation model and the population/expectation audit. They SURVIVE G8. kb/Work PB2109 deleted
  `guard-compiler.sh`'s legacy arm, the legacy exemption list, the guard's legacy Unit and Integration legs, and
  the ad-hoc dashboards `compliance.sh`, `nist-batch.sh` and `run-suite.sh`.

### 3.8 CI (target `build-and-test.yml`)
An OS matrix, greenfield-authoritative, with the characterization gate:

```yaml
strategy: { matrix: { os: [ubuntu-latest, windows-latest] } }
jobs:
  build-test:      # per-OS: build -warnaserror; dotnet test Unit + Conformance + Characterization --no-build
  version-sweep:   # ubuntu: cobol check-batch INV-1 (permissive continuity), fail on BREAKS
  nist-cli:        # ubuntu: guard-fast.sh — the CLI-level WiseOwl COBOL NIST leg (PB750). Survives G8.
```

The `legacy-oracle` job is gone (kb/Work PB2108); post-G8 the two legacy test projects go too, and `build-test` becomes the whole gate,
identical on both OSes (NIST now runs in-process, closing smell #5). `Generated/` continues to regenerate per
checkout (a failed regen fails the build — keep).

### 3.9 Roadmap SSOT + DEVLOG discipline
- **`docs/REARCHITECTURE_PLAN.md`** (the master plan) — the resumable migration SSOT / ROADMAP: an ordered
  P0–P16 phase index, per-phase exit criteria (universal clause: "gates (1) & (2) green; gate (3) either unchanged or
  reviewed-re-baselined in this change set"), a top STATE banner naming the current phase, and the owner-decisions
  table. This is what a future engineer resumes the REARCH from; the plan §0 banner is the feature-drive live
  state and cross-links to it.
- **DEVLOG/** — one file per entry (§3.17.4, kb/Work PB3025), rendered descending; a real timestamp; one entry per
  landing on main, checked by `enqueue.py land` and CI through `python scripts/devlog.py check`, kb/Work PB2605) per the existing
  `feedback_devlog*` memories. Each rearch phase commit references its ROADMAP phase id.
- **`docs/DOC_INDEX.md`** — add rows for `REARCHITECTURE_PLAN.md` (the migration SSOT / ROADMAP),
  `DIAGNOSTICS.md`, this doc, and the sibling `DESIGN-*` / `PHASE-*` docs; keep the "one canonical doc per subsystem" rule.

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
| `tests/_shared/ProcessObservation.cs` | **THE one child-process observer.** Replaced six copies of "start `dotnet`, wait N s, return whatever came back" (`CutRunner.RunExit`, `AcceptDifferentialTests.AcceptRun`, `WiseOwlCobolTestBase.CompileAndRun`, three in `EndToEndTestBase`) plus a seventh found by its own drift guard (`BinderDecompositionTests`, which read both streams synchronously and then read `ExitCode` without checking `WaitForExit`'s result). A run that does not complete raises `HarnessNonObservationException` — it never returns partial output for a caller to compare. Retries **once, serialized**, first: that is re-attempting a measurement that did not complete, not re-rolling a failed assertion. A caller that runs a PROGRAM in a directory names it (`runDirectory:`), and the retry starts from the state that directory had before the first attempt (`RunDirectoryState`: files up to 1 MiB kept by content, larger ones by length and write time; a larger file the killed attempt changed makes the retry a non-observation, never a measurement over another input): a killed COBOL program has already created and rewritten files, and IX216A's OPEN EXTEND of an absent OPTIONAL file answered 00 instead of 05 over the leftovers of its killed first attempt (kb/Work PB3388). `EveryRunOfACompiledProgram_NamesItsRunDirectory` keeps every `dotnet <dll>` call site on it; `guard-fast.sh`'s serial re-observation already restarts in a fresh `mktemp` directory. **The budget is a rule over the WORK, never one fixed number** (`tests/_shared/ObservationBudget.cs`, kb/Work PB3108): one harness unit (120 s, `WISEOWL_COBOL_RUN_TIMEOUT_MS`) for the observed process plus one per serial round of the child processes it runs, `1 + ceil(children / parallel)` units, so a program or a script gets one unit and `run.py` over a suite gets one per round of its compiles and runs (the two-runner drift test observes it once per edition, `--jobs` stated). The unit stays the floor for one program: 120 s against a recorded cost of at most 9.3 s for one suite test's compile and run and 7.0 s and 3.8 s for the two single-program tests PB3108 names, a multiple of 13 to 32 for load. A timeout reports the budget, the work it covered, the process's own processor time and the host's load across the attempt (`HostLoad`: `/proc/stat` and `/proc/loadavg`, `GetSystemTimes`), so load is told from a hang. Every retry and non-observation is appended to `WISEOWL_COBOL_HARNESS_LOG`, which the local gate (`run_gate_legs.py`'s `leg_env`) and every CI test step (`CiHarnessLogDriftTests`) point into their results directory and the Linux gate (`linux-gate.sh`'s `test_leg`) into its own directory on the Linux filesystem, copying it into `TestResults/linux-gate` as it ends (`remove_gate`: that directory is a Windows drive mounted over 9P, where an append beside a reader of the same file lost lines and the read failed with ENODATA, measured 2026-10-24, kb/Work PB3194), and CI uploads it with the TRX, so the rate is measurable and a CI non-observation is readable afterwards. |
| `ProcessObservationDriftTests` | Keeps the extraction collapsed (the `TestRepoDriftTests` pattern): no test source may start a process under its own bounded wait. Plus five behavioural facts, including "a process that never finishes RAISES instead of returning empty output" and "`Observe` reports a timeout with an **empty** stdout" — if that ever returns content, the defect is back. |
| `scripts/guard-nist-audit.sh` | The population + manifest + expectation audit, consumed by the guard so the rule is written once. A plain `divergent` row expects `MATCH` (PB750) and a `TERMINATES` row expects `TERMINATES <EC>` (PB1955), the second set asked of `guard-population.sh`; a verdict word it does not know (the retired `LEGACY DIVERGENT` included) is an UNRECOGNIZED finding. `--self-test` proves all sixteen checks can fail, including the two `divergent` arms and the five TERMINATES arms. `guard-fast.sh` runs it, with `guard-population.sh --self-test` and `guard-verify.sh --self-test`, before every guard run. |
| `scripts/guard-compiler.sh` | ⭐ **THE one answer to "which compiler is this gate measuring?"** (kb/Work/PB750): `cobol`. `guard_assert_compiler_identity` reads the resolved CLI's own `.deps.json` and REFUSES to run when the closure does not contain `WiseOwl.Cobol.Compiler`. Earned by the guards hard-coding the legacy CLI, so `guard NIST: 353 MATCH` measured the ORACLE for the whole rearchitecture and battery #58's NC215A wrong answer was invisible to it. `--self-test` (discovered by `scripts/self_tests.py`, and run by `guard-verify.sh` too) proves the refusal fires on synthetic manifests. |
| `scripts/guard-compile.sh` | The compile invocation for one NIST program, written once (`cobol` needs `--nist NAME` because its parser binds the next token). Called by the guard's parallel compile and its serial re-observation retry, two call sites that would otherwise each have had to be kept in step by hand. |
| `scripts/guard-verdict.sh` | ⭐ **THE evidence rules for the NIST guard, written ONCE and sourced by its one program runner** (`feedback_one_rule_one_place`). `guard_compile_verdict` (compile arm), `guard_output_verdict` (run + compare arms: normalization, candidate resolution, the FAIL*/footer rules, the verdict), `guard_preserve` (keep a non-MATCH's evidence). It reports through `GUARD_VERDICT` / `GUARD_CLASS` (`match` · `regression` · `no-verdict`) so each caller keeps its own recording and counting, and every function is option-local (`local -`) and returns 0: a scoring routine that can abort a `set -e` caller is not a scoring routine. **The comparison materializes both normalized sides into real files** — corollary 5 — and reads `diff`'s exit status explicitly. |
| `scripts/guard-baselines.sh` | ⭐ **THE baseline-cleanliness audit, written ONCE and called by the guard** (`feedback_one_rule_one_place`). Empty / `FAIL*` / non-zero-footer baselines are refused, unless the program is declared `CCVS-DEFECT` in `tests/nist/corpus.tsv` — the same declaration, read from the same file, that `CorpusManifestTests.GoldensCarryingACcvsFailure_AreExactlyTheDeclaredCcvsDefects` states as a set equality. The complement is audited here too (a declaration with nothing to describe is a defect). Exits with the FAILURE COUNT, so each caller keeps its own aggregation. |
| `scripts/guard-run-group.sh` | ⭐ **The ONE runner of NIST programs** (kb/Work PB3383): each named program in its own clean `mktemp` directory after exactly its declared predecessors. A serial twin, `scripts/guard.sh`, carried a COPY of the rules "kept character-for-character in step" by prose — which had already drifted (one `normalize()` had a `[ -f ]` guard, the other did not) and kept both copies of the compare-arm hole — and ran every program in ONE shared directory, so IX115A graded over the `tf024.txt` IX114A fenced; it is retired, and `guard-verify.sh` fails on any other guard script that runs a NIST program. Compile diagnostics are captured (`<TEST>.compile.log` + `.compile.rc`) instead of `/dev/null`; the run is bounded by `timeout` and its exit status kept instead of `\|\| true`; **any non-MATCH's report, streams and both normalized sides are copied into a run-scoped forensic directory** before the program's dir dies with it (attributing battery #43 cost hours because that directory was already gone). |
| `scripts/guard-fast.sh` | THE NIST guard. Runner stderr captured instead of discarded (a runner could die and take its verdicts with it in silence); the audit gates `ALL GREEN`. ⛔ **FULL FAN-OUT IS KEPT AND THE LOST OBSERVATIONS ARE RE-TAKEN INSTEAD** — capping `-P` would pay for the damage on every run to protect against something the evidence rules now DETECT. Contention can no longer corrupt a verdict, only lose one, so step 3b re-runs serially just the lost programs and the members that declare one of them as a predecessor. See this table's isolation row below. |
| `scripts/guard-fast.sh` isolation | ⭐ **Every program is its own job** (kb/Work PB3334, PB3383): its runner gives it a clean directory and runs its DECLARED predecessors (`corpus.tsv` `chain-preds`) there first, so no grouping and no order carries a chain, and a non-predecessor cannot touch a consumer's file at all. It replaced the chain graph's connected components (332 groups over 376 programs, each serial in one directory), which had replaced a hand-written "these six suites run serially" list. Justified by evidence, not guessed: `NistDifferentialTests` runs every asserted program in a per-program directory with only its declared predecessors and is green. A missing predecessor edge shows as a consumer DIFF, never a false green. ⚠ **The leg is THROUGHPUT-bound on `dotnet` cold-start**, not tail-bound: measured on a 32-core Windows box, shortening the longest serial group from 40 to 9 moved the NIST phase from 564 s to 598 s (~150 s of it the compile phase, effective concurrency ~7). **The real lever is a persistent run-host to amortize cold-start**; the isolation is kept for correctness and for retiring a hand-maintained list, not for speed. |
| `scripts/guard-verify.sh` | `--self-test`, discovered by `scripts/self_tests.py` (every gate and CI's audits) and run again by `guard-fast.sh` before it measures. **The evidence-rule witnesses** — synthetic runs through the REAL `guard-run-group.sh` over a fake repo root with `dotnet`/`diff` shims on `PATH` and a fake `corpus.tsv`, one rule per case, no corpus and no compiler: absent report → NO-VERDICT, 0-byte report → NO-VERDICT, `diff` forced to exit 2 → NO-VERDICT, `rc≠0` with empty stderr → NO-VERDICT, **a genuinely wrong report → REGRESSION** and `rc≠0` *with* a reason → REGRESSION (the discriminators, without which the fix could turn every regression into a NO-VERDICT and look green), plus the compile arm, the FAIL*/footer rules, the written-files and termination arms, the forensics, **the run directory** (a consumer is not corrupted by a non-predecessor that wrote its file, and gets its declared predecessor's file when run alone — both red against the pre-PB3334 shared-directory runner — with a discriminator proving the consumer needs the file) and the structural checks (no process substitution in a verdict path; one copy of the rules; one runner of NIST programs). Re-run them against an old runner with `GUARD_GROUP_RUNNER=<old copy>`. It used to run an **equivalence proof** too, diffing the retired serial guard's verdicts with `guard-fast.sh`'s: RELATIVE, so two guards sharing PB473's hole agreed, and red for the serial guard's shared directory rather than for any difference in what a verdict means (kb/Work PB3383). |
| `scripts/gnucobol_differential.py` | A rejection needs a non-zero exit **and** a diagnostic; an acceptance needs the artifact. Evidence-free compiles are retried once and then bucketed `NO_COMPILER_EVIDENCE`, which counts as a harness failure and is **named for re-run**, never folded into a divergence bucket. Its population is no longer its own: it filters with `gnucobol_extract.differential_cases()` before dispatch, so `len(payload)` IS `cases run` and the corpus has one definition. |
| `scripts/gnucobol_extract.py` | **THE external-corpus population, as an API** — `primary_source` (the one "is this a case" predicate, returning the `(member, compile-check)` pair the caller actually needs so nothing re-derives half of it), `differential_cases` (the 1,323), `iter_programs` (the 1,611 COBOL members, what a sweep must read). Both readers call it; there is no second place to define the corpus. |
| `scripts/corpus_sweep.py` + `ExternalCorpusPopulationDriftTests` | The reachability instrument and its lock. The sweep prints a per-population census on EVERY run and refuses to report hit counts when its population check fails. The drift test asserts the sweep's live external population equals the differential's COMMITTED per-case baseline — two independently produced numbers, so agreement is evidence. It was **proved failing first**, driven against the old `*.cob` reader, where it reported `{"external": 2, "baseline": 1323, "state": "drift"}`; `TheDriftCheck_ActuallyFails_WhenTheExtractionIsEmptied` keeps that red permanently reachable. A missing interpreter or an absent corpus is a LOUD failure, never a skip. |
| `scripts/fetch-gnucobol-tests.ps1` | **THE fetch the population gate depends on, and a gate in its own right.** It broke all three of its own contracts at once (`kb/Work/PB897`): it `Remove-Item`d `tests/external/gnucobol` BEFORE an extraction that then failed, it passed the archive as a drive-letter path that GNU tar reads as `host:path` (`Cannot connect to E: resolve failed`, exit 128) while bsdtar accepts it, and Git-for-Windows’ GNU tar cannot read a `.tar.xz` at all (it execs an `xz` it does not ship). Which `tar` was first on PATH therefore decided whether the corpus existed — so the two `ExternalCorpusPopulationDriftTests` reds became permanent and every implementer brief told agents to ignore them, which is a gate that has stopped gating. Now: extraction lands in a STAGING directory and is swapped in only after tar succeeded **and** the payload was verified (nothing is ever deleted before its replacement exists); the archive is named RELATIVE to its own directory, which neither tar misreads (⛔ `--force-local` is the wrong repair — bsdtar rejects it outright); and the tar is chosen by ATTEMPTING the work down an ordered candidate list rather than by trusting `Get-Command`. Every failure prints one `FETCH FAILED: <cause>` line and exits non-zero, and `build-local.{ps1,sh}` both surface it: the gate goes RED with `EXTERNAL CORPUS FETCH FAILED, POPULATION UNMEASURED` on the verdict line. `-SelfTest` drives the REAL `Invoke-CorpusExtraction` over a synthetic `.tar.xz` — including the failure branches — and `ExternalCorpusFetchTests` runs it; it was proved failing first against the old shape (4 of 8 checks red under GNU tar, and 4 of 8 under bsdtar too, since the delete-first fault is tar-independent). |
| `scripts/external_corpus.py` | **THE one rule for obtaining the corpus in a tree** (`kb/Work/PB2611`): present when `tests/external/gnucobol/tests/testsuite.src` exists, otherwise the fetch script runs there, and a fetch that exits non-zero, exits 0 with no corpus or cannot start is ONE named red, `EXTERNAL CORPUS FETCH FAILED, POPULATION UNMEASURED`. The gate driver (`Host.fetch_corpus`), `battery.sh` (phase −½, before the Unit leg and the differential), `record_impact_map.py`, `linux-gate.sh` and the cloud session hook all apply it; battery #89 read NOT GREEN at a green tree because `battery.sh` alone had no fetch. Its `--self-test` drives every arm and `ExternalCorpusFetchTests` runs it, and `OnlyTheCorpusRule_InvokesTheFetchScript` keeps any other script under `scripts/` from naming the fetch script; CI's two fetch steps are the recorded exception (a fresh checkout never holds the corpus, and the step's exit status is the red). |

**What this does not claim.** The invariant makes a false GREEN and a false RED *visible*; it does not by itself
prove the battery is deterministic. That is the measurement A12/A12d asks for, and it is recorded in plan §11
beside the row, not here.

### 3.11 TEST-COLLECTION PARALLELISM — the partitioned-theory mechanism (plan §11 A13)

> **A test CLASS is a scheduling unit, not just a namespace.** xUnit 2.9.2 parallelizes at TEST-COLLECTION
> granularity and by default **each test class is one collection**, so every test in a class — including every
> row of a `[Theory]` — runs SERIALLY ON ONE THREAD. A fat class caps an assembly's whole wall clock, and
> nothing in the normal output says so: `dotnet test` reports totals and a duration, never concurrency.

**The instrument.** `scripts/profile-test-parallelism.py <run.trx>` reads the trx `scripts/battery.sh` already
emits and prints, per class, `tests / sum(s) / span(s)` plus the assembly's average concurrency. Read the two time
columns together: `sum ≈ span` means the class ran serially and is a splitting candidate; `span << sum` means it
was already spread across threads.

**The mechanism** — `tests/_shared/TestPartitioning.cs`, linked into every project under `tests/`.

xUnit v2 offers exactly two levers and only one of them can split: `[Collection]` MERGES classes into a shared
collection, so it can never divide one. That leaves genuine class splits — and the naive class split duplicates
the test bodies, which is how a split rots. So the tests live ONCE in an abstract generic base and each partition
is one line:

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

Row `i` belongs to slot `i % Partitions` — a STRIDE, not a contiguous block, because theory rows are ordered by
construct or program name and adjacent rows have correlated cost; a block would concentrate the expensive rows in
one partition.

⭐ **Why it works:** static members of a CLOSED generic type are per-type-argument, and xUnit resolves
`[MemberData]` against `testMethod.DeclaringType`, which for a method inherited from `FamilyTestsBase<Slot3>` is
the *closed* type, not the open definition. That was PROVED with a standalone probe on xunit 2.9.2 /
xunit.runner.visualstudio 2.8.2 before any real class was touched — 3 partitions over 9 rows produced 9 tests, 3
per partition, each asserting its own slot; open-type resolution would have produced 27 tests and 18 failures.

**⛔ The invariant, and its gate.** A partitioned family FAILS OPEN: delete one partition class and the rows it
owned are simply never run — no error, no red, just a smaller and entirely plausible test count.
`TestPartitionAudit`, run per assembly by `TestPartitionCoverageDriftTests`, closes that. It is SHAPE-DRIVEN, not
registered — it finds every family by structure (a top-level, author-written, abstract generic base with one type
parameter constrained to `ITestPartitionSlot`), so a NEW family is covered the moment it is written. Four checks,
one per way a family loses rows silently:

| check | what it catches |
|---|---|
| the row source yields NOTHING | every check below would compare an empty union against an empty source and report green |
| slot indices ≠ {0 … `Partitions`−1} | a deleted, duplicated or mis-slotted partition class |
| an EMPTY partition over a non-empty source | more partitions declared than the source can fill — the one waste the union check cannot see, because the surviving slots still cover the whole set |
| union of the partitions ≠ the source as a MULTISET | rows dropped or double-run, each named in the failure |

Plus a ladder check: `Slot9.Index => 8` would silently put two classes in one partition and corrupt EVERY family
at once, so the slot ladder is verified against its own names, once, centrally.

⚠ **The gate was proved failing, not trusted silent** — and it earned that on its FIRST real run, before any
deliberate break: Roslyn emits a nested `<>O` delegate-cache class inside any generic type that caches a lambda,
and a nested type inherits its enclosing type's generic parameter *with its constraints*, so every family produced
a phantom family with no `Partitions` const. Both structural checks were then fired deliberately (a partition
class commented out → the slot-set violation plus a per-source "87 row(s) NEVER RUN" naming the exact dropped
rows; a slice count desynced from the const → `TestPartitioning.Slice`'s own range guard throws first, which is
why that direction cannot reach the audit at all).

#### 3.11.1 What is partitioned, and what it measured

| family | `Partitions` | before (battery #41 trx) | after |
|---|---|---|---|
| `VersionMatrixTests_P0 … _P11` | 12 | 2127 rows, **720.5 s SERIAL** — the whole 721 s leg | 12 × (175…179 rows), 552–601 s each, all spanning the run together |
| `NistDifferentialTests_P0 … _P5` | 6 | 349 rows, 237.9 s SERIAL | 6 × (58–59 rows), 329–354 s each |
| `ConformanceSuiteTests_P0 … _P23` (formerly `CorpusRunnerTests`) | 24 | at 3 partitions (2026-10-07, about 4,635 rows): 3 × about 1,546 cases, 246–282 s each, the Conformance leg's 95–220 s tail at 1–4 threads | 24 × about 193 rows (kb/Work PB2527); `ConformancePartitioningDriftTests` bounds a partition at 500 rows |
| `OptionalWordSubsetDriftTests_P0 … _P8` | 9 | one 23-row class, 131–152 s span (one row 59 s) | 9 × at most 3 formats (kb/Work PB2527; 9 since kb/Work PB3751's rows made 26), bounded at 3 by `ConformancePartitioningDriftTests` |
| `StorageFormNistEquivalenceTests_P0 … _P7` | 8 | one `[Fact]` looping the corpus, **171.5 s** — *was* the Unit leg's wall clock | 8 slice-`[Fact]`s, 81–138 s each |

Whole-corpus assertions stay OFF the partitioned base and run ONCE (`VersionMatrixTests`' two catalogue facts,
`ConformanceSuiteTests.TheAlternativeSignConventionTest_CarriesItsWiseOwlOption`) — an inherited `[Fact]` would run N times and inflate
the count N-fold. The StorageForm sweep is the one deliberate count change (6 → 13) and it keeps its POPULATION
assertion: the whole-corpus bar was `parsed >= 50`, and each partition now asserts its proportional share rounded
UP, so a partition that silently stopped binding goes RED instead of green-and-empty (§3.10 corollary 1).

**Measured, same box (24-physical/32-logical i9-13900K), same `Conformance ∥ Unit ∥ Characterization` shape:**

| leg | wall | sum of test time | avg concurrency |
|---|---|---|---|
| Conformance | 721 s → **600 s** | 1948 s → 10469 s | 2.7× → **17.4×** |
| Unit | 171 s → **138 s** | 329 s → 895 s | 1.9× → **6.5×** |

⛔ **THE TAIL IS GONE; THE WALL BARELY MOVED — AND THE SECOND HALF OF THAT SENTENCE IS THE FINDING.** A13
predicted ~783 s → ~80 s. The split did precisely what it was designed to do, yet bought 17%, because
`sum-of-test-time ÷ wall` measures COLLECTION concurrency, **not core utilisation**, and the "idle" cores were
never idle: one WiseOwl COBOL compile is internally parallel (Roslyn `Emit`) and a NIST row spawns a `dotnet` child
that is too. The proof is in the after-profile itself — the SAME work reports **5.4× more test time** because the
new collections found contention, not silicon; at 17.4× + 6.5× ≈ 24 threads on 24 physical cores the box is
saturated. **The class-split lever is therefore EXHAUSTED**, and the remaining lever is to reduce the WORK — the
persistent compile/run host of §3.10's `guard-fast` row and A13(c), not more parallelism. Kept for the balance
and for the drift gate, exactly as the `guard-fast` regrouping was kept for correctness rather than speed. ⚠ The
contention was since NAMED (§3.14.5): for check-only compiles it is one lock in the ANTLR lexer's ATN simulation,
reached on every token because predicated rules keep its DFA from caching (the start state, and every edge through a
predicate),
so the continuity partitions ran at ~3.8 busy cores, not a saturated host; the partition counts are re-measured
once M6 removes it.

#### 3.11.2 ⛔ Filters must key on the METHOD, never on `Class.Method`

A partition class is `VersionMatrixTests_P0`, so `FullyQualifiedName~VersionMatrixTests` still selects it and
`!~VersionMatrixTests` still excludes it — but `~VersionMatrixTests.Cobol85Program_…` can never match
`VersionMatrixTests_P0.Cobol85Program_…`, and **vstest answers a filter that matches nothing with a SILENT
GREEN**. Two CI filters were keyed that way: the three continuity shards, and the **INV-1-STRONG job**
(`~NistDifferentialTests.NistProgram_MatchesGolden`), which unlike the shard matrix has **no population guard**
and would have kept reporting success over ZERO goldens. Both now key on the method name alone — unique in the
tree, and immune to the next re-partition. The shard-population job (§3.8) is what catches this class of error for
the sharded leg, and it was re-run locally against the split tree: 349+349+349+1080+1354+1761 = 5242 = discovered.

### 3.12 THE COMPILED-PROGRAM CACHE — reducing the WORK of a re-gate (kb/Work PB985)

§3.11 ended with the parallelism lever exhausted and "reduce the WORK" as the one left. The largest repeated work
is the lander's whole-Conformance re-gate after a TEST-ONLY fix (a probe template, a stale test-ref, a pinned
expectation — the dominant cause of trains 46–48's first-gate reds): ~7,900 cases recompiled although no compiler
or runtime bit changed. `tests/WiseOwl.Cobol.Tests.Conformance/CompiledProgramCache.cs` makes that re-gate reuse every
compiled program. **A hit still RUNS the program** — runtime behaviour is what a case checks; only the compile is
skipped.

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
(`build-local`, the lander) and OFF under CI (`CI` set — GitHub Actions always sets it); `scripts/battery.sh`
exports `off`, so **the battery and CI run COLD** (PB985 obligation 4 — the owner may decide otherwise).

**Measured (PB985 obligation 5)** — the whole Conformance assembly, 7,951 cases, three back-to-back runs on one
unchanged tree at `BelowNormal` priority on the shared 32-core host (2026-09-22): **cold, cache off: 22 m 53 s**;
**first gate, cache on and empty: 21 m 31 s** (12,886 compilations stored — the store's overhead is inside the
noise); **re-gate, warm, no product change: 1 m 27 s** (13,133 hits, 27 misses — the 6 WHEN-COMPILED programs and
the few harnesses whose sources live in a per-case random directory beside their copybooks — all 7,951 green in
all three). The compile, not the run, was the wall-clock pole under full parallel load: the populate run's
compilations summed to 25,217 thread-seconds (~2 s each under contention, against ~0.1 s serial), while the
program runs, being separate processes, spread across the cores. The whole store was 49 MB.

**⛔ A Debug build must not depend on the commit it was built at.** The three runs above shared one HEAD. The
lander's real re-gate COMMITS the test-only fix first, and the SDK then stamped the new commit into every
assembly (`AssemblyInformationalVersion` "1.0.0+&lt;sha&gt;", and the Source Link map whose PDB checksum the
assembly embeds) — every compiler bit changed, every case missed, and every incremental build recompiled every
project. `Directory.Build.props` therefore sets `EnableSourceControlManagerQueries=false` (and drops the revision
suffix) for **Debug only**; Release, the shipped build, keeps its stamp. `ADebugBuild_DoesNotStampTheCommit`
pins it on the built bits. Re-measured on the real flow — populate at one commit (14 m 0 s, a quieter host), then
a TEST-ONLY commit (that very test), rebuild, whole assembly: **build 5 s + 1 m 18 s, 13,133 hits / 27 misses,
7,952 green.**

### 3.13 THE PER-TEST IMPACT MAP — ORDERING the implementer's gate (kb/Work PB1683, PB1708)

**The defect it answers.** An implementer's gate ran the tests its author NAMED (its goldens, `~Drift|~EditionGate`,
`~CorpusRunner|~Nist` on a "shared seam"). Train 68b dropped two groups on whole-assembly reds those names never
reached — `DiagnosticPositionTests` after a COPY library-search change, and a `CONSTANT AS NULL` crash in
`DataBinder.Constants.cs` — and wave 69 (four agents, 1.17 M tokens) existed only to finish them. The map records
the fact the names were guessing: which tests execute which code.

⛔ **The map ORDERS the plan; it never FILTERS a leg** (owner, 2026-09-28, kb/Work PB1708). Every gate PLANS the whole
discovered population (§3.14) and the map only decides which tests come FIRST, so a red surfaces in minutes. Since
batched gating (owner 2026-10-10, kb/Work PB2515) leg 1 is the only local run, which makes the map's tiers decide what
a gate runs locally: a test the change reaches whose recorded time puts it in leg 2 runs only in CI's merge-group run
(PB3666's bounce, 2026-10-24: two goldens its change reaches sat in leg 2 and went red there). So the loop records a
map for EVERY main commit it adopts (owner 2026-10-24, kb/Work PB3663; below), and a dependency the map cannot see costs
a later red, never a missed one — the single-process, memoizing compiler hides dependencies in more ways than one
recorder can close (values memoized into static collections, edition state cached in the front end, static FIELD reads
— PB1712).

**Collection — a per-test hit recorder, chosen by measurement.** Per-test coverage from a coverage tool cannot
attribute hits under xunit's parallel collections, and per-shard coverage bisected to tests multiplies the run.
The recorder instead rides the test run itself, in its own RECORDING build, and costs one cold Conformance run:

| piece | what it does |
|---|---|
| `tools/impact/ImpactRecording.targets` | imported ONLY through `-p:CustomAfterMicrosoftCommonTargets=…` (nothing in the tree imports it, so no gated, battery or shipped build carries a probe); compiles `ImpactProbe.cs` into the greenfield compiler, front end, editions, runtime and CLI, and the three test assemblies; compiles `ImpactTestFramework.cs` into the test assemblies and names it in `[assembly: Xunit.TestFramework]` |
| `tools/impact/ImpactInstrumenter` (Mono.Cecil) | rewrites each covered assembly once: every method with sequence points calls `ImpactProbe.Hit(id)` on entry, `id` being its entry in the probe table — its file and the LINE RANGES it owns (its sequence points, merged across the lines between two of them unless another method's point lies between, so braces, `else` lines and comments belong to it and a lambda's body to the lambda); members with no sequence points are charged to a TYPE-LEVEL entry; `implies` names each entry's type-chain static constructors; every `Process.Start` is redirected to the probe's twin |
| `ImpactProbe` | one byte store per hit into the array of the running CONTEXT, an `AsyncLocal` every assembly's copy shares through `AppContext` data; a hit with no context lands in an AMBIENT array. In a child process (a compiled COBOL program loading the instrumented runtime) it records for the process and writes a hits file at exit, which the redirected `Process.Start` queued on the starting test |
| `ImpactTestFramework` | xunit's own framework with the message bus wrapped: `ITestCollectionStarting` / `ITestClassStarting` / `ITestStarting` install a fresh context synchronously on the flow about to run it (the same `TestRunner.RunAsync` then awaits the test, so the context flows into constructor, body and every task it starts, while parallel tests keep their own); `…Finished` saves it with the children folded in. A `BeforeAfterTestAttribute` cannot do this — it is handed only the `MethodInfo`, which cannot tell a theory's rows apart, and the corpus theories are ~3,400 of the ~9,300 Conformance tests. It DERIVES from the gate's `GateTestFramework` (§3.14.3), so a recording build still names one framework |
| `scripts/spec/record_impact_map.py` | a DETACHED worktree at the commit, the recording build, the instrumenter, each test assembly once with `WISEOWL_COBOL_COMPILE_CACHE=off` (a cache hit skips the compiler and would record nothing for it), and the merge into `<git common dir>/cobol-impact/<sha>.json.gz` — shared by every worktree of the repository. Its watchdog fails the recording when a listed test was never recorded or no test reached the compiler, front end or runtime; a recorded test takes the name the gate knows it by (`adopt_listed_names`: the vstest adapter truncates a long theory row's display name with `···`, xunit's own name is the full one), and the map is written beside its name and renamed into it, so a reader never opens half a file. `--self-test` covers both |
| `scripts/spec/impact_store.py` | THE ONE HOME OF THE STORE (kb/Work PB3663): its location (`<git common dir>/cobol-impact`), the map's schema and bitset codec, which paths are documentation, **which map serves a base** (`nearest`) and how the store is bounded (`prune`, `clean_work`). The recorder, the lookup and the loop's recorder lane each carried a copy of these before |
| `scripts/orchestrator/impact_lane.py` | THE LOOP RECORDS A MAP FOR EVERY MAIN COMMIT IT ADOPTS (owner 2026-10-24, kb/Work PB3663; `DESIGN-orchestrator-loop.md` §4.10): `request` (called by `adopt_main.py adopt`) starts one DETACHED worker at BelowNormal or coalesces to the newest commit; the worker holds the recorder's gate slot; a failed recording posts to the operator's mailbox and shows in every adoption line until one succeeds; `measure` reports the share of gate plans a map served |
| `scripts/spec/impacted_tests.py` | the lookup: `analyse()` → the change's TIERS (below) and its tier-0a facts (the test methods it adds, the corpus goldens it touches), which `scripts/gate_plan.py` computes in-process to assemble the plan (§3.14.2); `--base <cut point> --plan <file>` writes the same analysis as JSON for inspection. M11 deleted its selection (kb/Work PB1717): its last stdout line is ALWAYS the whole-assembly filter `FullyQualifiedName~.`, map or no map, until M13 deletes that line and `--plus` with every caller (§3.14.9) |

**Measured cost.** At `dbea12428`, `BelowNormal` on the shared 32-core host: 35,578 probe entries; Conformance
9,274 green in 15 m 19 s (cold, instrumented), Unit 29,693 green in 2 m 38 s, Characterization 33 in 2 s; build
23 s, instrumentation 4 s; **18.7 min in all**. The map was ~5 MB compressed. Re-measured at `7d769bcc1` (2026-10-25,
the loop's own recorder lane, BelowNormal): 48,426 probe entries, 57,270 tests (Conformance 18,832, Unit 38,402,
Characterization 36), build 84 s, Conformance 4 m 29 s, Unit 7 m 4 s; **13.6 min in all**, and the map is **25.2 MB**
compressed, five times the earlier figure, which sizes the store's bound (`impact_store.KEEP_NEWEST`). Three tests fail
in every recording, by design: `GateLegDriftTests` arm 4 asserts a test project names `GateTestFramework`, and a
recording build names `ImpactTestFramework` (which derives from it).

**What the lookup computes: a TIER per test.** A change is analysed exactly as PB1683 built it: a hunk inside a
method's owned lines reaches the tests that executed that method; a comment-or-blank hunk reaches nothing; an ADDED
ordinary method reaches the tests that reached any same-named method of the file's types (overload capture); a
DECLARATION change (an override, virtual, operator, conversion, extension method, field, property, constant, a type
with a base list or an attribute, or any removed or changed line no method owns) reaches every test that executed
the file plus every test that executed a file naming one of its outermost types, BESIDE the direct hits of the
file's other hunks (a signature change next to edited method bodies keeps those bodies' tests in tier 1; reading
stopped at the first declaration hunk until PB3869's bounce, which put every one of them in tier 2, outside leg 1);
a static constructor's change
reaches every test that touched its type. A test's reach is its own recorded context plus its class's and its
collection's (constructors, fixtures). The test assemblies are recorded too, so an edited theory harness reaches
exactly the rows that executed it (an edited test METHOD is also tier 0a, §3.14.2). From that:

| tier | the tests | runs |
|---|---|---|
| 1 | executed a changed method body (the direct hits) | first, cheapest first |
| 2 | reached only through a widening — a static constructor, a declaration change, the file level | next, cheapest first |
| 3 | reached by nothing the change touched | last |

What used to force the WHOLE-assembly filter — no map in the store, a `.g4`, project, build or source-generator input,
a src file the map has never seen, non-C# data under `src/`, a method reached only outside every test's context — now
puts EVERY test in tier 1, and the plan still contains all of them.

**Which map serves a base (kb/Work PB3663; `impact_store.nearest`).** A map is a statement about the tree it was
recorded at: per source file, the line ranges each method owns and the tests that executed them. It describes a base
EXACTLY for every file that is the same in both trees and not at all for one that differs. The earlier rule asked only
whether the map was older than the base by more than documentation and, when it was, called it STALE and put every
test in tier 1 — but main moves about 1.6 landings an hour (many more in a burst) and a recording takes 14-25 minutes,
so almost every base would have been stale. The question is instead which source files differ between the map's commit
and the base (`drift`): the store's map with the fewest drifted files serves the base, whatever its age or ancestry (a
map recorded after the base serves it too: the comparison is of the two trees). The plan's `map_state` says `exact`
(the map's commit is the base), `documentation-only drift`, `nearest` (source files differ) or `none`. A change to a
file outside `drift` is attributed by line range exactly as before. A change to a file IN `drift` is taken at the file
level: every test that executed any method of it is tier 1 (`impacted_tests.drifted_change`), and a file new since the
map's commit, which the map has never seen, puts every test in tier 1 with that reason printed. A test added since the
map's commit is not in it and is tier 0u (§3.14.2); a reach the map under-states costs a later red, never a missed one.

**Measured (2026-10-25, kb/Work PB3663).** One map, recorded at `315f9eb98`, replayed against the 44 landings that
followed it over 5.9 hours (the base of each: its first parent; 22 changed a file under `src/` or `tests/`, 22 only
documentation, kb, scripts or `.claude`): all 44 were served (1 `exact`, 43 `nearest`) where the earlier rule would have
read 43 of them stale; of the 22 code-changing landings 18 were attributed (tier 1 from 0 to 17,515 of 18,580 Conformance
tests, by what the change reaches) and 4 put every test in tier 1 (three grammar, project or generator inputs, one file new
since the map). The replay also found two rules that fell back for no reason a test could see, now fixed: a public-API
analyzer baseline (`PublicAPI.*.txt`) and the runtime ABI pin (`RuntimeAbi.pin`) are data a test may read and not "a non-C#
input under `src/`" (2 landings, and PB3666's own change, whose replay then reads 6,642 of 18,832 Conformance tests in tier 1
and both bounced goldens in leg 1 where it had read all of them), and a
file-level change is unattributable for running only outside every test's context only when NO entry of the file was
reached by a test (2 landings, both editing a file that holds a finalizer beside code every test runs). A hunk inside a
method that only the ambient array reached stays unattributable. `impact_lane.py measure` repeats the count over the
gates' own plans.
`ImpactedTestsDriftTests` (Unit) drives every arm of both `impacted_tests.py --self-test` and
`gate_plan.py --self-test`, proves through the real command line that a mapped change is tiered and that nothing
it prints is a filter (M13 deleted the filter line and `--plus`, kb/Work PB1721), pins the partition-suffix contract `NameKey` relies on (§3.14.2), and requires every
product project under `src/` to be in both the targets file and the recorder's `PROBED` list.

**What the replays measured — for this compiler the map mostly says "everything", so COST orders inside a tier.**
Every golden runs the whole pipeline. Over the 20 replayed clusters of trains 65–69, method granularity reaches
5,504–9,268 of 9,274 tests, and even a perfectly precise registry and declaration analysis leaves a median of 6,404
(evidence `impact-map-pb1708/a5`, `a6`). On the one dropped branch with named reds, 68b X, 9,144 of 9,190 tests are
tier 1 (`b4`). Ranking tier 1 by SPECIFICITY (the sum of `ln(N / df)` over the changed entries a test executed) put
X's first red after **62 %** of the assembly's recorded test-seconds — worse than the plain class-and-name order
(14 %) — because the change that broke them (the COPY library search) is executed by every test. Ranking by
recorded COST, cheapest first, put it after **0.32 %** (40 of 12,408 test-seconds). So inside a tier the order is
cheapest first, and the map's contribution is the tier boundary. Replayed through the order plan itself
(`gate_plan.py`, `b4`'s row P), both of X's reds are in leg 1 and the first runs after 0.32 % of the work, with leg 1
holding 2,829 cases (248 test-seconds, a 15.0 s floor). One replayed branch is a small sample; M13's
acceptance (§3.14.9) records the time to the first red of every implementer gate of the next train.

### 3.14 THE ORDERED WHOLE-POPULATION GATE, WHOLE-SUITE SPEED AND THE GATE CAP (kb/Work PB1708)

#### 3.14.1 The contract

- **Every gate PLANS the WHOLE discovered population** of Conformance, Unit and Characterization, every time, into
  two LEGS: the likely-red tests first, everything else second. Ordering changes WHEN a test runs, never WHETHER:
  under batched gating (below) leg 2 runs in CI's merge-group run, the whole-population regression net (§3.17).
- ⛔ **Sound by construction: every discovered test case runs exactly once across the legs.** A case's leg is a TOTAL
  function of its display name and one plan file (§3.14.2), so the legs cannot overlap and cannot leave a case out;
  the gate CHECKS it on every run (§3.14.4) and a drift test proves the check fires.
- ⛔ **Only the gate driver can make a test host filter.** The leg filter acts only on a complete, self-consistent
  environment the driver writes; any partial or stale one turns the run RED (§3.14.3), every other caller scrubs it,
  and every whole-assembly run — the battery and CI as well as the gate — asserts its population with one tool
  (§3.14.4). A filtered run can therefore never pass as a whole one.
- **Fail fast:** the gate stops at the first leg with a red and reports RED and INCOMPLETE. It never reports GREEN
  unless every leg ran. A gate is done only on GREEN — under batched gating, on `LEG 1 ONLY (batched gating,
  PB2515): GREEN`.
- ⭐ **Batched gating (owner 2026-10-10 10:19 PDT, kb/Work PB2515: "Yes, permanently", after a measured trial from
  2026-10-07).** The SCOPE is `leg1` by default (`gate_slot.DEFAULT_SCOPE`, §3.14.6; its setting keeps the name
  "implementer scope"): a gate whose plan has a leg 2 runs leg 1 only, names every leg-2 case NOT RUN through the population check, and prints
  `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN|RED — … ===` — never `GREEN`, so no reader takes
  it for a whole-population green. The owner's switch back is the shared scope `whole` (`gate_slot.py
  set-implementer-scope whole [--until ISO]`), under which the gate runs both legs and the fail-fast bullet above
  applies as written. The trial's measurement (the retired train records, read by `landing_measure.py summary` as its BEFORE column, 2026-10-10): batched 15 trains, 85 changes
  in 24.8 h = 3.43 landed per hour, 2.7 whole-population runs per train, 0 interaction reds, 1 CI red (an environment
  difference, kb/Work PB2563); whole 8 trains, 25 changes in 35.3 h = 0.71 per hour (confounded by the cap of 3 during
  the trial). CI's whole-population run is then the population check for every change; a red there is attributed
  per cluster (MANDATORY-PRACTICES L12) and every landing is measured (`scripts/orchestrator/landing_measure.py`, §3.17.5). A gate
  with no plan runs its one leg, the whole population, and says GREEN.
- **The verdict line counts the whole population, by discovered case:** `=== BUILD-LOCAL GATE: GREEN — Conformance
  9,317/9,317 · Unit 29,703/29,703 · Characterization 33/33 cases ran (skipped 0) in 2 legs (plan 3f1c…: map dbea124,
  timings run 20260928T2210-7c1e) ===`. A case "ran" when it has a Passed or Failed result; a case whose only result
  is NotExecuted (a `[Fact(Skip)]`) is counted SEPARATELY as skipped, never as ran (`b3`).
- **No plan input, a stale one, or a broken one → ONE leg in the plain order.** Never a skip. A previous gate's trx the
  plan cannot read is one such input, and it is NAMED and KEPT (kb/Work PB3661): every reader of a trx file reads
  through `test_population.trx_root`, which raises `UnreadableTrx` naming the file (the gate planner and the profiler
  each parsed one themselves, and a torn Linux-gate file ended run 000005 of wf_1973801d-4fe-29 with `ParseError: not
  well-formed … line 104295, column 36` and no path, the file gone by the verdict because the Linux half empties
  `TestResults/linux-gate/` before it writes). The gate driver copies the unreadable file into the run directory as
  `unreadable-<its directory>-<its name>.bad` (never `.trx`: later readers glob a run's trx files) and its plan note
  names both. The whole-population fallback stays for an unreadable SIDE source too: with the Linux gate's reds unknown
  the gate cannot know what was red, and leg 2 is not run locally (PB2515), so dropping that source's tier 0a could hide
  a red. `gate_plan.py --audit-trx-readers` (`GateLegDriftTests`) holds the tree to the one reader.
- **The gate has ONE shape** (owner 2026-10-11, kb/Work PB3015 Q1a, PB3022; §3.17.3): `build-local.ps1` takes no
  mode (`-Priority` only sets the process priority) — a plan, leg 1 fail-fast, a gate slot (§3.14.6), the worktree's
  gate lock and the population check, for an implementer's commit, a re-gate after a fix and a landing alike. The
  lander's former mode (no plan, ONE whole-population leg, no slot) is deleted: it was 7–53 min, 8 of 15 RED under
  load, on 2026-10-10. The battery and CI do not use the driver.
- **A gate outlives its caller, and a measured tree is gated once** (kb/Work PB3536; §3.14.12): it runs DETACHED in its
  run directory and its verdict file is the one thing any reader waits on, and a re-gate of an unchanged tree answers
  from the recorded GREEN, saying `REUSED`.

#### 3.14.2 The plan: `scripts/gate_plan.py`

The driver first LISTS each assembly (`dotnet test --list-tests`, 2.0 s for Conformance), so the plan assigns every
discovered case explicitly. Inputs, each optional: the diff against the worktree's cut point; the impact map that
serves the base (§3.13, `impact_store.nearest`: whatever its age); TIMINGS — the per-test durations in the trx files of this worktree's most recent COMPLETED
gate (its run directory, §3.14.3), else the map's recorded durations; and that gate's REDS (trx outcome `Failed`).

**The name key.** Every lookup — rank, tier, timing, red — keys on `NameKey(display name)`, never on the raw name,
because the raw name is not stable (`b7`):
- a partitioned family (`TestPartitioning.Slice`) puts row *i* in class `_P{i % Partitions}`, and that class is part
  of every row's name. Appending one golden to the 85 manifest shifts every later corpus row by one position, so
  **1,398 rows (450 test-seconds) change name** — to a raw-keyed plan all unknown, all forced into leg 1, and leg 1's
  serial floor rises from 46 s to **208 s** (one `CorpusRunnerTests` partition). `NameKey` removes the partition suffix;
  keyed that way, the append leaves ONE unknown test (the new golden) and the floor unchanged — re-run through
  `gate_plan.py` itself (`b7`'s last table): one tier-0u case, a 15.3 s floor. Appending to 2002 renames 838 rows, to
  2014 677; to 2023 or the negative manifest, only the sentinel.
- **The contract** (`gate_plan.py#name_key`; the test hosts' leg filter mirrors it exactly, M12): split the display
  name at its FIRST `(`; in the part before it, split on `.`; when there are at least three segments and the
  second-to-last (the CLASS) matches `(.+)_P\d+` in full, replace it with the capture; rejoin, and append the `(` and
  everything after it unchanged. A theory's arguments are never touched. Only a partition class may carry the suffix:
  `ImpactedTestsDriftTests` is red on any other `class …_P<k>` under `tests/` (a partition is exactly
  `class Family_P<k> : FamilyBase<Slot<k>>`).
- a display name must not embed a worktree path. 404 Unit rows (`ParenTokenTwinDriftTests`) carried the absolute
  source path, so the map recorded in a detached worktree and a gate in another never share those names (`b8`:
  `E:\\COBOL-wt\\battery87\\…` against this worktree's root). Substituting a placeholder for the root is NOT a fix:
  xunit truncates a long argument with `···` at a fixed length, so the cut point moves with the root's length. The
  test passes the repository-relative path instead (M12), and `GateLegDriftTests` arm (5) is red on any discovered
  name in a gated assembly that contains the repository root, so the next such test cannot recur.

**The tiers.**
- **Tier 0h — the host-only witnesses** (kb/Work PB3022, §3.17.3): every case whose class carries the xUnit trait
  `[Trait("Host", v)]` with v in `gate_plan.HOST_ONLY` — ONE table, today `ReFS` (the Windows file-system race
  witnesses, which CI's NTFS runners may not reproduce). Always leg 1, whatever the diff, exempt from the budget and
  the cap. **Tier 0c — the cheap tree-wide sweeps**: every case whose class carries `[Trait("Sweep", "tree")]` — the
  drift tests `drift_rules.py --sweeps` classifies as reading a whole tree — and costs at most `SWEEP_CHEAP_SECONDS`
  of recorded time; leg 1 within the collection cap. Both traits are read from the test SOURCES (`class_traits`, so
  the plan needs no build), and `HostWitnessTraitDriftTests` holds the scan equal to the compiled attributes, the
  Sweep set equal to the sweeps, and every race witness (a Unit class driving the sharing layer's host types from
  concurrent workers) traited `Host`. That class, `gate_plan.TRAIT_GUARD`, is itself tier 0h: its cases spawn the
  scans and cost 5-11 s under a gate's load, over `SWEEP_CHEAP_SECONDS`, so as a plain sweep it sat in leg 2 and the
  Windows leg 1 never checked the two tiers; two branches on 2026-10-16 gated GREEN there over a trait drift (a new
  sweep without the trait, a non-sweep with it) that the landing's whole-population Linux unit leg found (kb/Work
  PB2730's finisher). A tier the gate does not check at every gate is a tier on trust.
  **Tier 0d — the governing drift tests** (kb/Work PB3531): every case whose class the drift-rule index
  (`scripts/spec/drift_rules.py`) says governs a file the diff changes, specifically or as a tree-wide sweep that reads
  it — the rules an implementer is told to honor before it edits the file (MANDATORY-PRACTICES P11). Gathered from the
  diff alone (`impacted_tests.Reach.drift_tests`), with or without a map, and checked before 0c; always leg 1, exempt
  from the budget and the cap, so a dear sweep over a changed file runs there too. Why: a sweep of the COMMITTED tree
  (`TrackedTree.Files`) once filtered `git ls-files` in code and named no path, so the index never listed it; one of
  them (`CatalogCoverageDriftTests`, 44 s recorded) scans every tracked script, PB3523's branch edited one, the Windows
  gate left the case in leg 2 (dear, so not 0c) and the landing's Linux unit leg found the red. Such a sweep now reads
  the tree only through a declared `TreeScope` (`Under` / `Except` / `Kinds`, string literals in named arguments;
  `tests/_shared/TrackedTree.cs`), which the index reads from the source and applies exactly as `TreeScope.Holds` does;
  `TrackedTreeScopeDriftTests` holds every scan to a declared field, every declaration as the index reads it equal to
  the value the compiler builds, and the index's `in_scope` equal to `Holds`. A sweep of the TEST sources reads them only
  through that reader too (kb/Work PB3814): a recursive listing of `tests/` or of the repository root from the file
  system descends into every running test host's `bin/` and `obj/`, where another test's file I/O creates and deletes
  directories while the walk runs (a delete-pending sidecar-lock directory answered the walk with access denied), so
  `TrackedTreeScopeDriftTests.NoTest_WalksTheTestsTreeFromTheFileSystem` refuses a recursive listing of the repository root,
  of `tests/` or of a test project's directory (a data-only directory such as `tests/nist` holds no build output). The root
  is FOLLOWED, not matched as spelled: `TestsTreeWalks` (a Roslyn syntax walk of the source) resolves a local, field,
  property, assignment, helper-method return, lambda parameter, `foreach` variable, call argument or theory row to the
  `TestRepo` expression it was built from, and the drift test holds the share of listings it can follow (a root from
  another file or built at run time is reported unresolved, never read as safe). (The walks of `src/` are a separate population: they enter the product projects' build
  outputs, and no failure there is measured.) A drift test that reads the tree through
  a HELPER CLASS names no path either (kb/Work PB3247: `ConstantNameLiteralPositionDriftTests` reads the whole corpus
  through `SuiteManifest`, and the register drift tests (since replaced by `work_inventory.py`) read the register through
  `WorkRegister`, so a golden or a note governed neither and the landing's Linux leg found the reds), so the index also reads every top-level type a drift
  test uses by name (a member access, `new`, `typeof`, a base type, `using static`; never in a comment), in its own
  test project or `tests/_shared`, and the drift test governs, transitively, every path those helpers name
  (`Sources.inherited`; the per-file query says `— through <helper>`). Those paths govern (tier 0d) but never make a
  class a sweep: the closure is file-grained, and a literal added to one helper must not move the `Sweep` trait of
  every class that uses it. `TestRepo`'s own path helpers are read from its source (`test_repo`), never copied: the
  copy the index once held lacked a helper (`Kb`, then), and `--check` fails on a helper in a shape it cannot read; a suite test named
  by its literal id (`TestRepo.SuiteCase`, `SuiteFile`) governs that program and its sibling files specifically. A
  helper call's path is its LEADING string literals (`TestRepo.Nist("cases", note + ".txt")` reads under `tests/nist/cases`; a
  collection expression is its elements), and `Path.Combine(TestRepo.Root, "a", ...)` is read as the root helper it
  spells out (24 such reads in 16 drift tests were invisible; seven of those classes scan a whole tree and now carry
  the `Sweep` trait). A call's path is what its arguments SPELL, read by one evaluator with balanced parentheses
  (kb/Work PB3112, `drift_rules.path_expr`): literals, `TestRepo.Root`, a class constant (`const string`,
  `static readonly string`; never a local or a parameter, which is another method's; a constant may name a later one however long the chain, resolved pass by pass to a fixed point bounded by the declaration count, kb/Work PB3645), a nested `Path.Combine(..)` or
  `TestRepo.X(..)`, up to the first argument it cannot spell. 62 of the 391 drift tests read a path the earlier
  single-level regex missed (`TestRepo.Src(Path.Combine("WiseOwl.Cobol.Frontend", "Grammar"))`, `Path.Combine(TestRepo.Root,
  GrammarDir)`, `Path.Combine("src", ..)`; five grammar-scanning classes governed no grammar file). A method that wraps
  one read (`Read(params string[] parts) => File.ReadAllText(TestRepo.At(parts))`) is read at its CALLERS, whose
  literals spell the path (only the arguments bound to the parameters the body's path carries: for
  `AssertDeclares(string script, params string[] cases)` the `script`, never the `cases`); the body's own directory read is replaced by theirs unless some use of the wrapper (a method
  group, `nameof`) spells none. That narrowing is exact, so three classes that were read as sweeps of `src` or
  `scripts/spec` are specific to the files they name and lose the `Sweep` trait, and one (`LevelNumberArmDriftTests`,
  which enumerates `src/WiseOwl.Cobol.Compiler`) gains it. A drift test that reads the repo and spells no path at all
  (`TestRepo.At(tree)` from a theory's parameter) governs no file, so `drift_rules.py --check` refuses it
  (`blind`): spell the trees with literals. With that check the index sees every read a drift test makes, and the
  tier-0c cost cap (instances three to five of PB3112: a load-skewed sample, a case never re-timed) decides only the
  ORDER of a sweep the diff cannot reach, so the timings store needs no change.
  **Measured cost of PB3112's change** (shared store, 2026-10-24, contention-inflated as below): a grammar edit
  governs 74 -> 78 classes, leg 1 476 -> 581 s (`SendingOperandDriftTests` 76 s and `ArithmeticOperandRoleDriftTests`
  24 s join); a compiler emitter or binder edit 130 -> 128 and 125 -> 124 classes, 357 -> 346 s and 404 -> 404 s; a
  golden-adding, test-editing, script-editing or `docs/CONFORMANCE.md` diff is unchanged (18, 17, 13 and 31 classes).
  **Measured cost** (the Unit assembly, the
  shared store's recorded seconds, no map, as at every gate on 2026-10-22; leg 1 before: 34,324 cases, 723 s, floor
  49 s): a diff to one script plus its DEVLOG entry and note governs 10 classes, leg 1 1,209 s, floor 111 s; this
  change's own diff 22 classes, 1,867 s, floor 154 s; four landed compiler fixes of 2026-10-22 (13–44 files) 123–173
  classes, 3,057–4,227 s, floor 224 s (`OperandClassScreenDriftTests`). The recorded seconds of these drift tests are
  contention-inflated (the nine classes this change touched ran their 60 cases in 2 s on an idle host against about
  430 s recorded), so the gate's measured leg wall is the cost that counts: this change's own gate ran leg 1 in
  118 s (Unit 111 s, Conformance 117 s, with 135 and 24 tier-0d cases) against 46 s (Unit 44 s, Conformance 46 s) for
  the gate run just before it on another branch, on the same host and the same shared timings.
  **A register's citations** (kb/Work PB3131): a drift test that reads a REGISTER by name — the traceability
  inventory's code-locations (`src/.../File.cs#Member`) and test-refs (`unit:Class.Method`), docs/CONFORMANCE.md's
  pins — and checks that its citations resolve is broken by a diff that never touches the register: one that deletes a
  cited member, renames a cited test or deletes a cited file. Tier 0d therefore also holds every class that governs a
  register citing what the diff REMOVES: `impacted_tests.removals` reads one `git diff -U0` (a deleted file; the
  identifiers on removed lines that the file's new text lacks; the types it declares) and `drift_rules.citing` looks
  each up in every non-C# file a drift test names, indexed once per plan by the three citation shapes (`Citations`).
  Four landings on 2026-10-14..17 stopped in CI's Linux unit leg on `SpecTraceabilityInventoryDriftTests` after a
  Windows leg 1 that left the class in leg 2 by its cost (4-6 s a case, over `SWEEP_CHEAP_SECONDS`); replayed with no
  map, each of the four commits (611bdb3a2, 684d01b4b, 1a473341f, c3c6879ab) now names the class, with the citation it
  breaks, at a plan cost of about 2-5 s. **A deletion is matched precisely**, because tier 0d sits outside the leg-1
  budgets (branch review R1-1 of PB3131 measured one prose comment in a deleted file promoting 102 classes): a type
  is one a C# line DECLARES (`TYPE_DECL`, a line's first token after its modifiers, never a comment, string or prose
  word), deleting one file of a `partial` type deletes no type, and a deleted type breaks a dotted name only where a
  member follows it (`Harness.Opens`, never `Program.cs` or `SourceKind.Program`); a deleted file breaks a path
  citation only when the citation, resolved against the tree before the change (`TreeFiles`: the files whose path ends
  with the citation's, or a golden pin's `<edition>/<test>` by name), names no file the change keeps, so
  `docs/README.md` survives deleting `docs/sub/README.md` and a bare word is no path. Of the six most recent
  file-deleting commits on main, five then promote nothing beyond their governed classes, and the sixth (cf9c85914)
  promotes the inventory's classes on a real break: its code-location
  `src/WiseOwl.Cobol.Runtime/IO/IndexedConnector.WholeStore.cs#StoreHolds` names the deleted file. A removed identifier that survives elsewhere in
  its file (in a comment) is not seen; a member declared in a deleted file of a `partial` type is not seen; a register
  outside the drift tests' named files is not read.
  `TIER_ORDER = ("0a", "0h", "0c", "0d", "0u", "1", "2", "3")`.
- **Tier 0a** — a case of a test METHOD the diff ADDS OR EDITS (matched by method name in the assemblies the file is
  compiled into): `impacted_tests.tests_changed` takes every test method whose lines — its attribute block, its
  declaration, its body — a `-U0` hunk of the diff touches, and every theory whose `[MemberData]` source a hunk
  touches (its rows are that member's values); a hunk of comment lines and blank lines outside every string literal
  touches nothing, decided from the LEXED source on each side (`cs_members.code_lines`, carried per line by the
  `Hunk`), never from a diff line alone, since a test's embedded COBOL program is a string whose lines may start `*`
  or `//` or be blank (PB3190 review R1-1; the method level's `method_ids` reads the same flags). The member spans
  come from `scripts/spec/cs_members.py`, a lexer that blanks comments, preprocessor lines, characters and every
  string form (verbatim, interpolated holes, raw) so the braces left are code; a source whose braces do not balance
  once lexed puts every test method of the file here, and `impacted_tests.py --audit-members` (run by
  `ImpactedTestsDriftTests`) holds the lexer to every source of the gated test projects (kb/Work PB3190: until
  2026-10-23 only an ADDED method was here, so under batched gating an edited test ran in leg 1 only if the tier-1
  budget reached it, and its red surfaced first at the landing's Linux gate or in CI); a corpus row whose manifest entry or golden files the diff changes; a case red at this worktree's previous
  gate on EITHER host: build-local's previous run, and the newest run of the Linux gate (`scripts/linux-gate.sh`, which
  a landing runs right after the Windows gate and which leaves its trx files in `TestResults/linux-gate/`), both read
  by ONE reader, `gate_plan.previous_gates` (kb/Work PB3155, PB3184), and a case red in the CI run that BOUNCED this
  branch's pull request out of the merge queue (kb/Work PB3644): that red lives in GitHub, not in the worktree, so
  before each plan `run_gate_legs.Host.fetch_bounce_results` asks the one GitHub reader
  (`queue_facts.fetch_bounce_results`) for the test artifacts of the newest FAILED `merge_group` run of the checked-out
  branch's open pull request (`gh run download`, `test-results-*`, one subdirectory per artifact) and puts them in
  `TestResults/ci-bounce/` (`gate_plan.CI_BOUNCE_RESULTS`), which `previous_gates` reads as its third source. The
  directory is replaced by the newest bounce's results (downloaded once per run: it holds a provenance marker), emptied
  when the pull request has no failed merge-group run (a cancelled group is no bounce), and left as it was when GitHub
  cannot be asked or the download fails (kb/Work PB3716: the download lands in a sibling `ci-bounce.fetching` directory
  that replaces the results only once whole, so a failed or half-done download neither deletes the previous results nor
  leaves a partial one, and the line it prints names the run the directory still holds); it orders the gate and never
  selects it. Every live tool that writes a trx file into a
  worktree is a row of `gate_plan.TRX_WRITERS` saying what the plan takes from it (the battery: nothing, it runs in its
  own detached worktree), and `GateLegDriftTests` holds the table equal to the tools (`gate_plan.py
  --audit-trx-writers`). Before it, a finisher's re-gate after a landing's Linux red ran neither failed case in leg 1
  and read GREEN (w1076a / PB2921). Always leg 1, exempt from the budget and the cap. The implementer's own new tests and goldens are here by
  construction. ⚠ **Corrected by the M11 acceptance (kb/Work PB1717):** this tier first held every case DECLARED IN a
  changed test file. 68b X edits the corpus and continuity harnesses (`CorpusRunnerTests.cs`, `VersionMatrixTests.cs`),
  so that rule put 5,774 cases and 8,037 test-seconds in tier 0a, and its first red ran after **62.8 %** of the work
  (`b4`, measured through `gate_plan.py`). An edited HARNESS — a helper the tests call — is tier 1 through the map
  instead, while an edited test method is tier 0a by its own span, never by its file's (the test
  assemblies are recorded, §3.13), where cheapest-first finds X's first red after **0.32 %**.
- **Tier 0u** — a case no timing source and no map knows (genuinely new, after `NameKey`). Each is charged the
  assembly's median recorded time and admitted to leg 1 within its OWN budget, `LEG_ONE_UNKNOWN_BUDGET` = 2 % of the
  assembly's recorded test-seconds, in declaration order; the overflow leads leg 2. A map far older than the base
  therefore cannot swell leg 1 without bound.
- **Tiers 1–3** — `impacted_tests.py` (§3.13); with no map, every case is tier 1; a case the map never recorded but a
  timing source knows is tier 1. A key several cases share takes the lowest tier among them.
- **Timings** are per case, by `NameKey`: the previous gate's trx duration, else the map's. Cases sharing a truncated
  name are charged their mean; a theory xunit cannot serialize is LISTED as one case (`Class.Method`) but RUN as rows
  (`Class.Method(item: …)`), so that listed case is charged its rows' sum. A case's COLLECTION is the map's collection
  for its raw class (so the `process-globals` classes share one), else the raw class — a partition keeps its suffix,
  being its own collection.
- **Leg 1** = tier 0a + the admitted tier 0u + the cheapest cases of tier 1 while their recorded time stays within
  `LEG_ONE_BUDGET` = 2 % of the assembly's recorded test-seconds AND the case's collection's budgeted leg-1 time stays
  within `LEG_ONE_COLLECTION_CAP` = 15 s (tier 0 is exempt from the cap). **Leg 2** = everything else.
- **Why the collection cap.** xunit runs a collection serially, so leg 1's wall is at least its largest collection's
  part, and on a GREEN gate every other core idles at the leg barrier until that part ends. Uncapped, a 2 % budget
  holds 2,883 of 9,217 Conformance cases and one `CorpusRunnerTests` partition holds 46 s of it; capped at 15 s it
  holds 2,740 and the floor is 15.2 s (`b7`, keyed by `NameKey`; `b5` measured the uncapped budget). The barrier's
  remaining green-path cost — that floor plus one more test-host start per assembly — is measured by M13 and counts
  against the cap's retirement criterion (§3.14.6).
- **Order inside a leg.** Collections by their best-ranked case (tier, then cost), and inside a collection its cases
  by the same key — the assembly-level `TestCollectionOrderer` / `TestCaseOrderer` of §3.14.3. In leg 2 the
  collections go LONGEST FIRST (largest recorded sum first), so a surviving long pole starts at once instead of
  capping the wall; with no timings, xunit's own order. **The timings come from the shared timings store** every
  gate publishes to (section 3.14.3 step 6; kb/Work PB2527), so a FRESH worktree's plan is timed too — before it,
  every implementer gate ran in a fresh worktree and printed `timings none`, and its leg 2 ran in declaration order;
  the worktree's own previous run times the plan only while no gate on the machine has published. Reds always come
  from the worktree's own previous gates (tier 0a, both hosts'), never from the store; the Linux gate's durations
  never time the plan (another host's).

**The plan file** (`plan.json` in the gate's run directory) holds, per assembly, the leg-1 KEYS, the leg-2 KEYS, the
per-key rank, and the plan's SHA-256. Its format (schema 1), written by `gate_plan.py --out` as canonical JSON (UTF-8,
no BOM, sorted keys, no insignificant whitespace):
`{"schema": 1, "base", "map", "map_state", "timings", "every_tier1": [reasons], "constants": {LEG_ONE_BUDGET, …},
"reds": {<source label>: {"keys": n, "listed": n}, …},
"assemblies": {"Conformance": {"legs": [1, 2] | [1] | [2], "leg1": [key, …], "leg2": [key, …], "stats": {…}}, …},
"sha256": …}`. `leg1` and `leg2` are in RANK order — a key's rank is its position, leg 1's list first — so the rank
needs no second structure; `legs` names the legs the driver invokes the assembly for; `stats` is for people (tier
counts, leg-1 cases, seconds and floor, the one-leg reason); `reds` names the two sources whose reds are tier 0a
(build-local's previous run under its timings label, `previous run: none` when there is none; the Linux gate's newest
run as `linux gate <its results directory>`; `gate_plan.previous_gates`) with how many red keys each gave and how
many of them name a case discovered here (`{"keys": 0, "listed": 0}` for a source that left none). Every plan the gate
driver or the CLI writes carries both, and the plan summary prints them. `sha256` is the SHA-256 of the canonical JSON of every
other field: the plan's identity on the verdict line and in the identity records. The handshake digest
`WISEOWL_COBOL_GATE_PLAN_SHA256` is the SHA-256 of the file's BYTES, which `gate_plan.py` prints as its last stdout line.
**The leg function:** `LegOf(case) = 1` if `NameKey(case.DisplayName)` is a
leg-1 key, `2` if it is a leg-2 key, and `1` if it is neither — total over every name, so a case the plan never
heard of runs in leg 1, never nowhere. Display names are the key's source because they are the one identity the
trx's test definitions, the map and `--list-tests` share (`b8`); they are NOT unique (xunit truncates long theory
arguments with `···`: in Unit one name covers four rows), so the function assigns every case of one name to the same leg, which
keeps it total and disjoint (`b3`: two rows sharing a truncated name both ran in leg 1).

**Degenerate inputs, decided PER ASSEMBLY.** No timings (neither a previous run nor the map knows any of its cases):
leg 1 is tier 0a alone, ordered by tier — nothing can be told new from old, so nothing is tier 0u. When an assembly's
leg 1 would be empty, or would hold its whole population, that assembly runs in ONE leg, and the driver does NOT
invoke it for the leg its plan leaves empty — an executor handed no case makes vstest print "No test is available",
no verdict line, and exit 0 (`b3`), which the leg report would score RED; the population check still covers that
assembly through its other leg. An assembly whose whole recorded time fits `LEG_ONE_COLLECTION_CAP` runs entirely in
leg 1: Characterization (33 cases, ~2 recorded test-seconds), whose 2 % budget alone would admit nothing.

#### 3.14.3 How the legs run on this runner (xunit 2.9.2 · xunit.runner.visualstudio 2.8.2 · Test.Sdk 17.11.1)

Each primitive the runner offers was measured (evidence `b3-order-probe/`: 24 collections × 4 facts, one theory whose
two rows share a truncated name, one skipped fact, on this repository's package versions; and on the Conformance and
Unit assemblies themselves, `b8`):

| primitive | verdict |
|---|---|
| `dotnet test --filter` naming the leg's rows | ⛔ rejected. A leg of ~3,000 theory rows needs one `DisplayName=` term each: about 400 KB at ~130 characters a term, beyond Windows' 32,767-character command line. The complement leg needs the De Morgan complement of the whole union. A value containing a space silently matched NOTHING and exited 0 (`DisplayName~edition: 2023`, measured). Truncated `···` names are not unique keys. |
| a runsettings `TestCaseFilter` | ⛔ rejected: the same expression language and the same defects, minus the length limit. |
| vstest.console `/Tests:` | ⛔ rejected: a SUBSTRING match on names (ambiguous by construction) and not reachable through `dotnet test`. |
| xunit `StopOnFail` (runsettings `xUnit.StopOnFail`) | ⛔ rejected: at the first red the test host CRASHED (`OperationCanceledException` in `XunitTestAssemblyRunner.RunTestCollectionAsync`) and vstest reported "Test Run Aborted". Two runs kept 0 and 6 of 99 results, never the red's — its message was lost, and the second run's summary read `Passed! - Failed: 0`. |
| assembly `[TestCollectionOrderer]` + `[TestCaseOrderer]` | ✅ used. Under 4 parallel threads the planned collections started in the first wave and each ran its planned case first; unplanned ones kept xunit's order behind them. |
| an executor that DROPS the cases outside its leg (`XunitTestFrameworkExecutor.RunTestCases` over a filtered list) | ✅ used. A dropped case leaves no trace in the trx — no result and no test definition — and the two legs' test definitions together equal `--list-tests` as a multiset, both rows of the truncated-name theory included (99 = 99). |
| a framework CONSTRUCTOR that throws, to refuse a bad environment | ⛔ rejected: xunit catches it and silently falls back to its default framework — every case ran, `Passed!`, exit 0. |
| an executor that throws | ⛔ rejected: exit 1, but "Catastrophic failure" with no verdict line and no trx result. |
| an executor that reports every case as xunit's `ExecutionErrorTestCase` carrying the cause | ✅ used to refuse: every case Failed with the message, a `Failed!` verdict line, exit 1. |
| `dotnet test --list-tests` | ✅ used for the population: 9,317 Conformance cases discovered in 2.0 s; the trx's test definitions of a full unfiltered run equal it as a multiset (`b8`). |

So the leg filter lives INSIDE the test assemblies, where the test cases are objects rather than strings:
`tests/_shared/GateLegs.cs` (linked into every project under `tests/` by `tests/Directory.Build.props`'s `_shared`
glob, as `TestPartitioning.cs` is) holds `GateNameKey` (the name key, mirroring `gate_plan.py#name_key`), the plan
reader, `GateTestFramework : XunitTestFramework`, whose executor runs `testCases.Where(c => plan.LegOf(c) == leg)`,
and the two orderers (landed, kb/Work PB1719):
- **The framework attribute is written by `tests/Directory.Build.props`**, for every project with `IsTestProject`,
  as an MSBuild `AssemblyAttribute` (`Xunit.TestFrameworkAttribute("WiseOwl.Cobol.Gate.GateTestFramework",
  "$(AssemblyName)")`): xunit resolves the framework by ASSEMBLY name, which a linked source file cannot spell, and
  on any miss it silently runs its default framework — every case, no filter. `GateLegDriftTests` arm (4) checks it
  in each gated assembly.
- **The plan names an assembly by its key**: the test assembly's name without `WiseOwl.Cobol.Tests.` (`Conformance`,
  `Unit`, `Characterization`), as the impact map and `gate_plan.py --list` do.
- **The orderers are installed by the executor's assembly runner**, after xunit reads the assembly's own orderer
  attributes and only under a plan, so a run with no handshake keeps xunit's order exactly (an orderer attribute
  would also need the assembly name, and would act on every run). The collection orderer ranks a collection by its
  best-ranked case; the case orderer ranks a class's cases by their own rank; unplanned ones keep xunit's order after
  them. The executor also hands the runner the leg's cases in rank order, because xunit runs a collection's classes
  in the order their first case arrives. ⛔ Each orderer's output is CHECKED to be a permutation of its input
  (`GateOrder.Permutation`) and throws otherwise: xunit trusts an orderer's result, so a dropped case would silently
  never run, while a thrown orderer is logged and xunit keeps its own complete order.
- **Any failure while choosing the leg's cases** — reading the handshake, the plan, writing the identity record, or a
  defect in that code — refuses the run the same way as a bad handshake below; the executor never throws.

**The environment handshake** — three variables, all written by the driver:
`WISEOWL_COBOL_GATE_PLAN` (the plan file), `WISEOWL_COBOL_GATE_LEG` (`1` or `2`) and `WISEOWL_COBOL_GATE_PLAN_SHA256` (the driver's
digest of that file).
- **None set** (the battery, CI, a gate with no plan, an IDE): every case, xunit's order, exactly as today. An
  EMPTY value counts as unset (Windows cannot hold an empty environment variable at all).
- **All three set and consistent** (the file reads, hashes to the digest — compared case-insensitively — and parses
  as a schema-1 plan naming this assembly; the leg is `1` or `2`): the leg's cases, in plan order.
- **Anything else** — one or two of them set, the file missing, unreadable or unparsable, a digest mismatch, a leg
  other than `1` or `2`, a plan of another schema, one that names no entry for this assembly or puts one key in both
  legs: the executor
  reports EVERY case as an `ExecutionErrorTestCase` naming the cause
  (`GATE ENVIRONMENT INCOMPLETE: WISEOWL_COBOL_GATE_LEG is set but WISEOWL_COBOL_GATE_PLAN_SHA256 is not`). The run is RED, never
  a silent whole or partial run (measured on the real hosts, evidence `m12`: each of eight partial or stale
  environments gave `Failed!` and exit 1; vstest keeps one failed result per test METHOD, because the error cases of
  one method share its id, so Characterization's 35 cases show 4 failed results). A developer who exports two of the variables by hand to reproduce a leg therefore
  gets a red battery, not a green one with a third of Conformance missing.
- ⛔ **Every caller of `dotnet test` scrubs its environment** — the three handshake variables, and with them the two
  OTHER environment channels that narrow a run while it exits 0 (M14, measured 2026-09-28 on Characterization's 33
  cases): MSBuild imports every environment variable as a property, so `VSTestTestCaseFilter=FullyQualifiedName~Snapshot`
  ran 31 cases, and `RunSettingsFilePath` (or `VSTestSetting`) naming a runsettings file with a `TestCaseFilter` did
  the same — in the run AND in `--list-tests`, so a population listed in the caller's environment is blind to them.
  The rule is written once, `test_population.py`'s `is_scrubbed`: the prefixes `WISEOWL_COBOL_GATE_` and `VSTest`, and
  `RunSettingsFilePath`, case-insensitively. A python caller passes `env=scrubbed_env()` (`record_impact_map.py`,
  `filter_population.py`; the driver merges its handshake over the scrubbed environment); every other caller runs
  `python scripts/test_population.py scrubbed dotnet test …` — `battery.sh`, `gen-vcr.ps1`,
  `gen-diagnostics-doc.ps1`, `guard-fast.sh`, `measure-battery-determinism.sh`, and the gate command
  `record_verdicts.py` prints. It runs the command rather than printing names to unset, because a PowerShell script
  run in the operator's session would otherwise delete the operator's own variables. **The CI workflow scrubs by
  construction**: a GitHub-hosted job's environment is exactly what the workflow declares, so it scrubs by setting
  none of those variables. `GateLegDriftTests` arm (6) runs `test_population.py audit-callers`, which is red on a
  `dotnet test` in command position under `scripts/` (the hooks excepted: they receive commands as data), on a
  python `["dotnet", "test", …]` without `scrubbed_env`, on a printed unscrubbed instruction, and on a workflow that
  sets a scrubbed variable. A console marker printed from inside the test host was considered and not adopted: what
  vstest shows of a host's output at `--verbosity quiet` is unmeasured, while the refusal arm and the population
  check below are measured.

**Each leg host writes its IDENTITY RECORD** into the gate's run directory — the plan's directory — as
`leg-<n>-<assembly key>.json`, before it runs a case (written to a temporary name and moved into place): `schema`
1, `assembly`, `leg`, `plan_sha256` (the digest it verified), `plan_content_sha256` (the plan's own `sha256`),
`test_assembly` (file, MVID and SHA-256), `product_assemblies` (the SHA-256 of every `WiseOwl.Cobol.*`, `Legacy.*` and
`cobol.dll` beside the test assembly: the compiler under test), `received` (how many discovered cases the host was
handed) and `runs` (the name key of every case it runs, in run order). The recording build's `ImpactTestFramework`
derives from `GateTestFramework` — overriding only its one extension point, `WrapMessageBus` — and the recording
targets define `IMPACT_RECORDING`, whose presence in `DefineConstants` drops the `tests/Directory.Build.props`
attribute, so an assembly always names exactly one framework (MSBuild evaluates item conditions after every property,
so the props file sees the targets' constant).

**The driver** is `scripts/run_gate_legs.py` (landed, kb/Work PB1721). `build-local.ps1` and `build-local.sh` (no
mode since kb/Work PB3022) only set the process priority and call it; their `Leg` functions and their `filter_population.py` call are
gone (a gate without a vstest filter has no term to prove live; `filter_population.py` stays for CI's shards, the
generators and `record_verdicts.py`, which still filter), and so are their two copies of the audit list: the driver's
`AUDITS` is the gate's one list (battery PHASE -1 and CI's `audits` job run the same scripts): the citation,
evidence-supersession, rendered-view, witness-loss, rule-catalog and spec-correction audits, `SELF-TESTS`, the
`WORK REGISTER` (`work.py check`) and `FLEET PRACTICES` (`check_practices.py`), the last two added after train 1067's
PR 9 went red in CI on a work-register finding behind a green gate (kb/Work PB1957 row 44). `AUDIT_PREREQUISITES`
makes the tree ready first exactly as the CI job does (`git submodule update --init tools/claude-skills`, which
`check_practices.py` reads); a failed prerequisite is RED and no audit runs. `SelfTestDiscoveryDriftTests` maps every
command of CI's `audits` job to an AUDITS entry, a prerequisite, or its recorded reason to run elsewhere: the one is
`devlog.py check`, a LANDING check (an implementer's entry stays `DEVLOG/pending-*.md` until `enqueue.py` stamps it,
and the check refuses a pending entry), run by `enqueue.py checks` and by CI on pull requests and merge groups. They run CONCURRENTLY
before the slot, each audit's output printed as one block. One entry, `SELF-TESTS`, is every script self-test under
`scripts/`: `scripts/self_tests.py` DISCOVERS them (a script that handles `--self-test`, read from its syntax tree, or a
file named `test_*.py`/`test_*.ps1`), runs them in parallel, each with a private `COBOL_COORD_DIR` and a private git
global config (on Windows also a TEMP that is a junction to the run's own scratch, `aliased_temp`, because CI's Windows
runner hands every process the 8.3 alias `C:\Users\RUNNER~1\...` as TEMP and a test comparing an unresolved TEMP path
with a resolved one is red only there otherwise; PR #714's bounce, kb/Work PB1957 row 25), and reports a self-test marked `# SELF-TEST-PLATFORM: windows — <reason>` as NOT RUN elsewhere with its
reason. Its post-build half (`self_tests.py --built`: the self-tests marked `# SELF-TEST-NEEDS: build`,
`filter_population.py`) runs beside the legs once the build is done. The same runner is CI's (the `audits` job on Linux,
`windows-self-tests` on Windows, `greenfield-unit` for `--built`) and the Linux gate's (`selftests` and
`selftests-built`), and `SelfTestDiscoveryDriftTests` holds every one of those entry points to it and forbids any of
them to name a self-test by hand (kb/Work PB2563: eleven orchestrator self-tests had run in no gate, and
`landing_check.py --self-test` only in CI). Measured 2026-10-08 on a loaded Windows host: the runner takes about 80 s,
set by the eleven parallel `test_orchestrate_*.ps1` parts, against the old serial audit phase's 42.5 s; on Linux the
44 self-tests that run there take 5 s. An implementer gate REUSES the recorded PASS of every self-test whose inputs are
unchanged, every self-test takes a place in one machine-wide budget, and a hung one is HANG, never RED (§3.14.10,
kb/Work PB2914). One gate:
1. **The worktree's gate lock** — an exclusive OS lock on `<worktree git dir>/cobol-gate.lock`, held from before the
   build to the verdict. A second gate in the same worktree REFUSES at once, naming the holder's pid: its build would
   otherwise overwrite the binaries between the first gate's legs. Its verdict is `BUSY` (exit 3), neither RED nor
   NOT RUN: it measured nothing and nothing is wrong, so a caller waits for the lock (kb/Work PB3527: the landing read
   it as a red); `run_gate_legs.read_verdict` is the one reader of a verdict line (GREEN, BUSY, NOT RUN with its
   cause, or RED). NOT RUN (exit 2) is a gate that measured nothing; it prints `NOT RUN [<cause>]`, and
   `run_gate_legs.NOT_RUN_CAUSES` classifies each cause ONCE (kb/Work PB3526; review R2-1 of wave 1150). ENVIRONMENT
   (outside the code the gated tree carries; no finisher fixes it): `[settings]` malformed shared gate settings, at the
   start or while the gate queued; `[interpreter]` no runnable Python, or one without PyYAML; `[host]` no WSL; `[sdk]`
   no .NET SDK in the Linux environment; `[tree]` the tree's git state unreadable or its clone failed. BRANCH (a failure
   of code the gated tree carries: a landing gates the branch merged with main and runs that tree's own scripts):
   `[driver]` the gate driver failed; `[population]` the population could not be listed after a GREEN build;
   `[arguments]` the gate script refused its arguments; and, made by `enqueue.py`, `[verdict]` no verdict line and
   `[unslotted]` a gate row that would run outside the shared slot; a NOT RUN naming no known cause is `[unclassified]`,
   BRANCH. Two more rows classify the landing's merge of main, which is no gate but can fail the same two ways
   (kb/Work PB3673, step 2 of section 3.17.2): `[merge-blocked]` ENVIRONMENT, `[merge-failed]` BRANCH. The driver's self-test holds every NOT RUN line of build-local.ps1, build-local.sh and linux-gate.sh, and
   its own, to a known tag. A landing reads an ENVIRONMENT cause as no red of the branch (exit 2, no BLOCKED line:
   retried once, a second is the operator's handoff); a BRANCH cause blocks like a red (exit 5, a finisher's) unless
   main fails the same way at the same step (`enqueue.gate_main`: the same row in a clean checkout of the origin/main
   the branch merged, once per main head and host, recorded in the common git dir's `enqueue-main-gate.json`; then exit
   2, the operator's).
2. A fresh RUN DIRECTORY `TestResults/build-local/<n>-<UTC stamp>/`: the plan, every trx, every identity record
   and the verdict file. Nothing is written to a fixed name, so no earlier gate's file can be read as this one's. The
   next gate's reds come from the newest run directory holding a verdict file (its timings from the shared timings
   store, step 6); the driver keeps the last five. "Newest" is the run's NUMBER `n`, one past every run in the run
   root, taken under the run root's lock (`runs.lock`) when the run directory is made, so the order of runs is the
   order they started in and never the clock's: a stamp orders nothing finer than the clock's resolution, and on a CI
   runner three runs started in the same microsecond (kb/Work PB3536, merge-group run 39888287569). The reuse rule
   (3.14.12) and the pruning read the same order; a run named before the numbers (`<stamp>-<nonce>`) is older than
   every numbered one.
3. The audits — FIRST, since none needs a build or a test result, and FAIL-FAST: a red audit ends
   the gate `RED — <audit> RED — fail-fast: the audits run before the slot and the build and NO LEG RAN` in seconds
   (kb/Work PB2523: four of wave 1034's eight first-run implementer reds were audit-only, each reported after a whole
   ~6-min population). The FIRST red STOPS the audits still running — each with its whole process tree, so the
   self-test runner's children go with it — and reports them `STOPPED`, never RED (kb/Work PB3061: wave 1062's agents
   A and B had CITATIONS RED at 23 s and their verdict 5–7 min later, after the self-tests; a stopped self-test run
   loses nothing, since every self-test that finished has recorded its PASS and the re-gate reuses it). (The deleted lander mode, kb/Work PB3022, ran them BESIDE its build and legs instead, kb/Work
   PB2880: ahead of the build they were a 42-s serial valley at about a third
   of the host in every landing, set by the self-test runner's long poles) — then the per-worktree
   GnuCOBOL corpus by THE one rule `scripts/external_corpus.py`, which `battery.sh`, the Linux gate and the impact-map
   recorder apply too (absent in a fresh worktree, so fetched; a failed fetch makes the gate RED as `EXTERNAL CORPUS
   FETCH FAILED, POPULATION UNMEASURED`; kb/Work PB2611). **Both run
   BEFORE the gate slot is taken** (kb/Work PB2524): they read only the tree, nothing the slot rations, and inside the
   slot they were 35-45 % of its hold (about 230 s of serial Python per gate while every other implementer gate
   queued; `verdict.json` of 22 gates, 2026-10-07). `audit_doc_citations.py`, the costliest, rules on every quotation
   IN-PROCESS over one loaded standard (kb/Work PB2525: one `cite.py` process per citation was 75 of its 79 s).
4. The gate slot (every gate, section 3.14.6), timed as `slot_wait_s` — taken BEFORE the build, so the
   cap bounds the builds too. The lock order is always worktree lock, then slot, so no two gates can wait on each
   other in a cycle. Shared gate settings that turn malformed while a gate queues stop it `NOT RUN [settings]`, with
   the reason.
   Then the solution build and the binaries' SHA-256 record — per assembly, the test assembly and every product
   assembly beside it, the set each identity record names.
5. `--list-tests` per assembly (the three concurrently), the plan — any planning failure, like no plan input, gives
   ONE leg in the plain order, named on the verdict line — then per leg the three assemblies CONCURRENTLY (the
   battery's `Conformance ∥ Unit ∥ Characterization` shape), each `dotnet test --no-build --logger trx` with the
   handshake (`leg_env`: the scrubbed environment with all three variables merged over it, or none — a partial one
   is refused in the driver itself), each printed through `test_leg_report.py`; an assembly the plan gives no case in
   a leg is not invoked for it. A red leg 1 stops an implementer gate there: the population check names every case
   of the uninvoked legs `NOT RUN` (all of them in `not-run-<assembly>.txt`, the counts on the verdict line).
6. The population check (§3.14.4); the publish of every measured duration of this run to the SHARED TIMINGS STORE,
   `<git common dir>/cobol-gate-timings/timings.json` (kb/Work PB2527; `gate_plan.publish_timings`,
   a stopped or leg-1-only gate too: a case it did not run keeps its earlier duration, a case no longer discovered is
   dropped by name key; a failed write is reported and recorded in `verdict.json` as `timings_store`, never in the
   verdict, because timings change only the order); the identity check (each leg host's record names the digest the driver handed it,
   the plan's content digest, the binaries step 3 hashed, the whole discovered population as `received`, and only
   its own leg's keys in `runs`), then the verdict line and `verdict.json` (the mode, the slot, the plan, every
   leg's per-assembly wall time and exit code, the time to the first red, and each population).

`-Filter` was removed from both scripts and every caller in M13 (kb/Work PB1721), with `impacted_tests.py`'s filter
line and `--plus`. The driver's `--self-test` fires every arm of §3.14.4 (3) against a planted host (a fake
machine whose leg hosts write the trx, log and identity record the real ones write); the partial-handshake refusal
on a real leg host is the C# handshake arm plus M12's measured evidence, and the driver's half is that it never
hands a partial handshake.

#### 3.14.4 Soundness: the population check — ONE tool for every whole-assembly run

`scripts/test_population.py` (a module and a CLI) answers one question for any run: did the union of its trx files
execute exactly the discovered population? Its callers are the gate driver (per assembly, over both legs: the module's
`list_population` and `check`), the battery (`battery.sh` PHASE 1, all three assemblies — `check --project … --trx …`;
until M14 it accepted any `Passed!` line with exit 0), and CI's `conformance-population` job (`check --listing … --trx
…` over each platform's shard trx files; the inline `grep -c` count block and the `shard-count-*` artifacts were
deleted in the same change, because a count cannot see a dropped test offset by one that ran twice). Each CI shard
lists its OWN build's population (`test_population.py list`, 2 s) and uploads it beside its trx as
`test-results-<OS>-conformance-<shard>`, so the checking job needs no .NET and no build of its own, and the listings
of one platform must be identical — the shards ran one build — or the check is an error. It also owns the ONE parser
of a `--list-tests` output (`parse_listing`); `filter_population.py` and `record_impact_map.py` import it.
- **The population** is `dotnet test --list-tests` — vstest discovery, which never calls the executor and so cannot see
  the leg filter — listed by the tool itself in a SCRUBBED environment (§3.14.3), because the VSTest environment
  channel narrows discovery exactly as it narrows the run.
- **The executed set** is the multiset union of the trx files' TEST DEFINITIONS (`<UnitTest name>`, one per
  discovered case that executed). Not the RESULT names: xunit lists a theory whose data it cannot serialize as ONE
  case and then reports one result per row, so Unit's results carry 33 names `--list-tests` never printed and miss 2
  it did, while its 29,703 definitions equal the listing exactly (`b8`).
- a name short → `NEVER RAN` (red, each named) — unless the run stopped early, when the remainder is reported as NOT
  RUN and the verdict is already RED/INCOMPLETE;
- a name over → `RAN TWICE` (red, named);
- a definition the listing never printed → `NOT IN THE POPULATION` (red, named: a trx of another build or assembly);
- a definition with no Passed or Failed result → counted as SKIPPED on the verdict line, never as ran;
- an input it cannot read (a missing or non-trx file, a listing without vstest's marker, an EMPTY population,
  listings that disagree) → exit 2, an error, never a finding. Each run prints one line,
  `=== POPULATION <label>: EXACT — 9,317/9,317 cases ran (skipped 0) across 1 trx ===` or `RED — …` followed by the
  named cases;
- (gate only) an IDENTITY MISMATCH → red: two legs read two plans (digest), or a leg ran binaries other than the ones
  the driver built and listed (the identity records' hashes against step 3's), so totality no longer holds.

`GateLegDriftTests` (Unit) proves it fails: (1) `LegOf` is total and deterministic over synthetic cases with
repeated, unknown and truncated names and an empty plan, and `NameKey` strips exactly the partition suffix; (2) each
orderer's output is a permutation of its input, and a planted non-permutation throws; (3) `run_gate_legs.py
--self-test` and `test_population.py --self-test` fire every arm on planted inputs — a dropped case, a duplicated
case, a skipped case, a digest mismatch, a binary changed between legs, a red in leg 1 (stop, remainder named,
RED/INCOMPLETE), no plan (one leg, plain order), an assembly whose leg 1 is empty (not invoked; its population still
whole), a partial handshake on a real leg host (every case an execution error, RED), a second gate in the same
worktree (refused), and ONE SHAPE (every gate takes a slot, its audits ahead of it, and stops at a red in leg 1);
(4) every test project under `tests/` that
the gate runs links `GateLegs.cs` and names `GateTestFramework`; (5) no discovered display name in a gated assembly
contains the repository root. Arms (4) and (5) audit the assembly they run in (`tests/_shared/GateLegAudit.cs`: the
attribute, and xunit's own discovery with theories pre-enumerated, checking each name and each string argument,
since a truncated argument can cut the root off; the root is matched as TEXT in its written, `/` and xunit-escaped
forms, never through the host's path rules, and Unit's witness plants a Windows, a POSIX and the host's own root, so
the arm is proven on each OS CI runs), so every gated assembly carries a `GateLegDriftTests` with those
two arms; Unit's also holds (1), (2), (3)'s population half, (6), the handshake arms and (4)'s structural half over
every test `.csproj`; (6) every `dotnet test` caller scrubs `WISEOWL_COBOL_GATE_*`, `VSTest*` and `RunSettingsFilePath`
(`test_population.py audit-callers`, §3.14.3) — the driver included, which merges its own handshake over the
scrubbed environment. Each arm is one test method named for its number, so the mechanisms that land them separately
merge by union.

#### 3.14.5 Whole-suite speed — measured first

An uninstrumented whole-Conformance run (battery #87, `b1`): 9,054 tests, **571 s wall**, 11,553 test-seconds.

| share of test-seconds | method |
|---|---|
| 54.4 % | `VersionMatrixTests.Cobol85Program_StillCompilesAtLaterEdition` — the continuity cells, 1,047 rows |
| 18.1 % | `NistDifferentialTests.NistProgram_MatchesGolden`, 349 rows |
| 10.8 % | `CorpusRunnerTests` (negative 6.3 %, positive 4.5 %) |
| 3.3 % | two single tests: the corpus row `pb505-table-value-dynamic-span-levels` (266 s, inside the corpus share above) and `ValueFormat2Tests.DynamicWithoutTo_HigherSubscriptsShallBeEqual_1946` (116 s) |

**The critical path is the twelve continuity partitions:** eleven of them ran 525–557 s each and all twelve ended
in the run's last 26 s, against a 361 s floor for the whole assembly's work on 32 threads. A continuity cell is two check-only compiles of
one NIST program (permissive, strict) at a later edition.

**Why a partition is slow — one lock in the lexer (`b6`, `b2`).** One partition alone: 0.97 s per row. All twelve
in one process: 369–395 s, ~4.5 s per row, with the test host using only ~3.8 of 24 cores. Server GC: 360 s. The
same work as two concurrent processes: 209 s; four: 136 s — the limit is INSIDE one process. An in-process probe of
exactly that work (`b2-scaling`) peaks at 3.5× by 8 threads, with ~1,400 contended `Monitor` acquisitions per
compile. Stack samples name it: **28 of 30 single-thread samples, and all 24 twelve-thread stacks, are in ANTLR's
`LexerATNSimulator.ComputeStartState` / `AddDFAState`** — half of the latter blocked in `Monitor.Enter`. ANTLR 4.13.2
`LexerATNSimulator.MatchATN` caches a mode's start state only when its closure carries no semantic context, and
`CobolLexer.g4`'s default mode has six predicates on the LEFT EDGE of a rule (`FN_SIGNED_FLOATLIT`,
`FN_SIGNED_COMMA_FLOATLIT`, `FN_SIGNED_DECIMALLIT`, `FN_SIGNED_INTEGERLIT`, `DEFINED`, `FNARG_SEPARATOR`). So every
token of every compilation recomputes the closure over all default-mode rules and re-adds it under
`lock (dfa.states)`. That is a PRODUCT defect: the `cobol` CLI pays it serially, and any host compiling in parallel
(the test host, an IDE) pays the lock too.

- **M6 — after warm-up, lexing performs NO ATN simulation.** Moving a predicate off the left edge is NOT enough.
  ANTLR 4.13.2 (decompiled `LexerATNSimulator`) never stores a DFA edge whose target configuration set passed
  through a predicate (`AddDFAEdge(from, t, q)` returns before `AddDFAEdge(from, t, state)` when
  `q.hasSemanticContext`), so every token that REACHES a predicate re-runs `ComputeTargetState` and `AddDFAState`
  under `lock (dfa.states)` — on every occurrence, not once. With `DEFINED`'s predicate one character in, s0's `D`
  edge stays uncached and every D-initial default-mode token (DATA, DIVISION, DISPLAY, DEPENDING, D-initial names)
  still takes the lock; `FNARG_SEPARATOR`'s does the same for every `,` and `;` separator, and the `FN_SIGNED_*`
  rules' for every `+` and `-`. So each predicate LEAVES the lexer's hot paths:
  - `FNARG_SEPARATOR` and `DEFINED` match exactly the text their fallback matches, so an ACTION picks the outcome
    (`[,;] [ \t\r\n]+` always lexes as one token; the action keeps it as `FNARG_SEPARATOR` in function arguments
    and skips it otherwise, exactly as `COMMA_SEP` / `SEMICOLON` and the whitespace rule skipped, and `COMMA_SEP`,
    which it subsumes, is deleted). `DEFINED` lost its rule: it is a virtual token that `IDENTIFIER`'s action
    retypes when the directive expression is primed. ⚠ The first cut — an unconditional `'DEFINED'` rule retyping
    itself to `IDENTIFIER` — lexed identical tokens but published the literal name `'DEFINED'`, which
    `CobolKeywordTokens` reads as "the lexer makes this word a keyword token" (so `>>COBOL-WORDS` would have
    treated DEFINED as one) and `CobolWordsDriftTests` flagged; a token-stream differential cannot see a
    vocabulary consumer, so this is recorded here (DESIGN-frontend-grammar.md §3.3e). Actions do not suppress
    DFA edges.
  - the four `FN_SIGNED_*` rules lex ONE token where the fallback lexes TWO (the sign, then the number), so an action
    cannot choose between them by retyping alone. They stay unconditional DEFAULT-mode rules, and their action
    (`OnSignedLiteral`) keeps the signed literal where `SignedLiteralCanStart()` holds — now asked of the character
    before the TOKEN'S START — and otherwise CUTS the token back to its sign: the type becomes `PLUS`/`MINUS`, the
    input is re-seeked to the character after the sign and the column restored (no numeric body contains a line
    break), so the lexer resumes at the digits and lexes them exactly as the predicate-false path did. The
    alternative, a mode entered by the preceding token's action, was rejected: the context is "a separator
    character immediately before the sign", which a mode would have to re-derive on every token.
  `LexerDfaCacheDriftTests` pins the invariant that matters: after one warm-up pass over the suite's sources, no
  ASCII character of a re-lex leaves the cached DFA, and `s0` is non-null for every mode. ⚠ The instrument is NOT
  a counting `LexerATNSimulator` subclass as first designed: in ANTLR 4.13.2 `MatchATN`, `ComputeTargetState` and
  `ExecATN` are not virtual, so no subclass can count them. It is a probe on the INPUT side — `Lexer.NextToken`
  and `LexerATNSimulator.Match` each `Mark()` the `ICharStream` as a match begins, and the test's stream, at that
  moment, walks the cached DFA of the lexer's current mode over the coming characters exactly as `ExecATN` will and
  records the first place the walk would leave the cache (no start state; a missing ASCII edge; a character above
  127; EOF). The lexer runs over a PRIVATE DFA, so the measurement is independent of what other tests lexed first,
  and the warm-up pass must itself see misses, or the probe is blind. The next predicate on a common prefix is
  therefore red, wherever in the rule it sits — measured: the pre-M6 grammar is red (5,216,645 start-state misses
  on the re-lex, 13 min against the fixed grammar's 3 s), and a predicate planted one character into `DEFINED`
  (`'D' {true}? 'EFINED'`) is red (68,289 mid-token misses — every D-initial token). ⚠ ANTLR's DFA edges cover only
  characters 0–127 (`MAX_DFA_EDGE = 127`), so a non-ASCII character is simulated on every occurrence, and the EOF
  transition is never cached (`GetExistingTargetState` rejects `t < MIN_DFA_EDGE`), so the token that reaches the
  end of an input is simulated once per input; the test REPORTS both, never asserts them. Over the lexer inputs the
  suite actually produces (17,055 distinct preprocessed texts and fragments): 14,556 ASCII inputs re-lex with zero
  misses; the 2,499 non-ASCII ones (38,675 non-ASCII characters in 72.8 M) leave the cache 77,196 times — against
  9,955,488 tokens. The implementing change proved equivalence once: every distinct (lexer input, priming) the
  Conformance, Unit and Characterization assemblies lexed on the pre-M6 grammar (compile cache off) was captured and
  replayed through both lexers, and all 17,055 token streams — type, channel, offsets, line, column, text, lexer
  errors and final mode — are identical (DEVLOG). Measured gain (`b2-scaling/b2-after-m6.txt`): the one-thread
  wall of `b2` fell from 110.1 s to 3.4 s (32×, beyond the 15× Amdahl bound estimated from the stack samples), at
  12 threads it is 2.0 s (55× today's one-thread wall: acceptance MET) with ZERO contended `Monitor` acquisitions;
  CPU utilisation at 12 threads is 21 % (acceptance NOT met) and the reason is no longer the lexer — 1.3 s of the
  2.0 s is workstation-GC pause, and the same run under server GC reaches 87 %. `b1` is re-measured on the next
  battery.
- **Sharing one front end across editions is REJECTED** as unsound: edition state is cached in the front end
  (`Frontend.LexAndParse` primes the lexer with the cell's `EditionInfo`; the reference-format and COPY
  preprocessors and `CobolParserCoreBase` hold per-edition state), so a shared front end would answer the 2014 cell
  from another edition's parse. M6 changes no test's inputs.
- **Process sharding is not built.** It bought 1.8× (two processes) and 2.8× (four) only because each process has
  its own lock; building it would route around the product defect (CLAUDE.md rule 4). After M6, `b6` is re-run: if
  two processes still beat one by more than 15 %, the next in-process serialization is found and fixed at its root.
- **Server GC** bought 3–9 % while the lexer lock bound the process: not adopted then. After M6 it is the next
  in-process serialization — `b2` over the whole green set spends 3.1–4.0 s of each ~5 s round paused in
  workstation GC at 8–24 threads, and reaches 6.1× at 87 % CPU on 12 threads under server GC
  (`b2-scaling/b2-after-m6.txt`) — so it is re-decided as its own mechanism, not inside M6. **Adopted (kb/Work
  PB2526, 2026-10-07):** `tests/Directory.Build.props` sets `ServerGarbageCollection` for every test host after the
  w1033 diagnosis measured 69 % of a 32-thread compile probe's wall in workstation-GC pauses, and the whole
  Conformance leg 318 s to 255 s and Unit 74 s to 56 s with identical pass sets; each gated assembly's
  `GateLegDriftTests` asserts its host's runtimeconfig carries `System.GC.Server`.
- **The compiled-program cache** (§3.12) already stores check-only results, so a TEST-ONLY re-gate hits every
  continuity cell; a gate after a product change misses by design. No change.
- **§3.11's "the class-split lever is exhausted"** was measured under this lock. After M6, the partition counts
  (one `Partitions` constant per family, audited by `TestPartitionAudit`) are re-measured; the first re-measure
  (w1033, kb/Work PB2527) took `CorpusRunnerTests` from 3 partitions to 24 and split `OptionalWordSubsetDriftTests` 8
  ways (section 3.11.1).

**M7 — the long poles.** `pb505-table-value-dynamic-span-levels` (266 s in the battery; **29.6 s alone through the
`cobol` CLI** for a 26-line program) and `DynamicWithoutTo_HigherSubscriptsShallBeEqual_1946` (116 s) are one
product defect: all three stack samples of the CLI compile sit in `TableValueOdometer.Resolve`, whose fill loop is
bounded only by its `MaxFillElements` ceiling (64,000,000) — and when the odometer can never carry out of a DYNAMIC
dimension with no TO, the phrase's TO is never reached, which is exactly the §13.18.63.3 SR23 violation ("the values
of subscript-1 and subscript-2 corresponding to all levels higher than that of the OCCURS clause, if applicable,
shall be equal") that `COBOL1946` then reports. The fix checks SR23 before any fill and bounds the
fill by the elements the phrase names; the test keeps asserting the diagnostic. **Landed (kb/Work PB1716):**
`TableValueOdometer.ElementCount` computes a phrase's element count as the mixed-radix distance from subscript-1 to
subscript-2 BEFORE anything is read and returns none for a phrase SR20/SR21/SR23 reject, so the `MaxFillElements`
cap is gone (and since kb/Work PB1722 `Resolve` keeps the PHRASES, never filling an element map at all); the SR23 rule's shape ("a level above ANY unbounded
dimension") lives once in `TableValueOdometer.UnboundedBelow`, read by both the fill and the `COBOL1946` screen,
which had tested only the OUTERMOST unbounded dimension and so accepted two nested ones spanned at the outer level.
pb505 now rejects in 0.49 s through the CLI (was 25.3 s on the same build). The other poles over 30 s in `b1`
(`CompiledProgramCacheDriftTests.TheOutputDirectory_DoesNotReachTheOutput` 55 s, `OptionalWordSubsetDriftTests`
`special-names-85` 41 s, `St101A` 30 s, continuity rows of IX113A/NC177A/NC253A/RL101A at 40–49 s) are compile-bound
and are re-measured after M6 before any is touched. The battery summary prints the five slowest tests (a REPORT,
never a wall-clock assertion — MANDATORY-PRACTICES forbids those), so the next pole is seen when it appears.

**The w1033 gate-cost levers (kb/Work PB2524–PB2527), measured 2026-10-07.** The diagnosis
(`E:\COBOL-coord\scratch\reports\w1033-perf-gate-report.md`) found a typical implementer gate holding its slot
430–940 s, 35–45 % of it the twelve serial audits, and the Conformance leg ending in a 1–4-thread tail of three corpus
partitions. Four levers landed together; each was measured on the same 32-core host, the arms alternated, the
compiled-program cache off:

| lever | before | after |
|---|---|---|
| audits run before the slot (PB2524) | about 230 s of each slot hold (94–358 s, 22 gates) | 0 s in the slot: gate 2 `audits_s` 28.8 s before `gate-slot: took slot`, `slot_wait_s` 0.1 s |
| `audit_doc_citations.py --check` in-process (PB2525) | 78.2 s (843 `cite.py` processes) | 2.5 s, byte-identical `--all` output over the corpus (949 citations) |
| the twelve audits, serial | 113.7 s (diagnosis, 25 % host load) | 28.3 s (`CITATIONS` 18.5 s is now the largest) |
| whole Conformance assembly, xunit order (PB2526 + PB2527) | 171.9 s, 196.5 s | 95.7 s, 68.0 s (10,917/10,917 passed) |
| whole Unit assembly (PB2526) | 76.0 s, 92.8 s | 49.0 s, 65.8 s |

The host was otherwise near idle (about 10 % load before the runs); the load sampled during each run is mostly the
run's own and is recorded with the evidence in `docs/rearchitecture/evidence/w1033-gate-levers/` (`measure.txt` and
the script that took it, `audits-after.txt`, and the doc-citation audit's `--all` output before and after). The
implementer gate
on the branch: gate 1 (fresh worktree, cold, `timings none`) 381 s wall, 333 s of it in the slot; gate 2 (a re-gate,
warm compile cache, timed from the shared store) 161 s wall, 123 s in the slot, Conformance leg 2 88.7 s. Not
adopted, left to the owner as the diagnosis' levers 4 and 5: a shared location-independent compile store, and a
ReadyToRun runtime for the programs the tests run.

#### 3.14.6 The gate cap (M2)

**Long holds are flagged, never killed (kb/Work PB3310).** The OS frees only a DEAD holder's slot, so a hung gate keeps
its slot for as long as its process lives (2026-10-16: a Linux gate whose WSL clone was deleted under it held one of the
three slots 6 h 40 min). Every released hold records its duration per kind (`gate_slot.hold_kind`: `gate`, `Linux gate`,
`record_impact_map`) in `<git common dir>/cobol-gate-slots/holds.json` (the last 50 per kind); `gate_slot.py status` marks a
held slot older than 5 times the median of its kind's recorded holds (at least 5 recorded) `LONG HOLD`, and the session
probe prints those lines. The limit is the gates' own history, not a fixed one, and the flag is advice: the operator
verifies the holder's pid before stopping anything. The Linux gate (`scripts/linux-gate.sh`) runs one gate per tree: a
second run on a tree whose gate is running answers `=== LINUX GATE: BUSY ...` (exit 3) before it touches the tree's
results, and `enqueue.py land` holds a per-worktree land lock (§3.17.2 step 3), so the overlap that produced the hang
cannot start.

`scripts/gate_slot.py` is ONE counting semaphore for the whole repository, served FIFO:
- **Slots.** N lock files `<git common dir>/cobol-gate-slots/<k>.lock`, each held by an exclusive OS file lock
  (`LockFile` on Windows, `flock` on Linux), so a crashed holder releases its slot when it dies and no stale-pid
  cleanup exists. The holder writes its label (who, pid, since when) at the start of the file; on Windows the lock
  covers one byte far past the end of the file, so the label stays readable. The label is informational only: a slot
  is free exactly when its lock can be taken.
- **Tickets.** A waiter first takes a TICKET: under an exclusive lock on `cobol-gate-slots/tickets.lock` it reads and
  increments the monotonic counter in `tickets.seq` (rewritten atomically; a counter that is not a number is an error,
  never a silent reset, since a reset could hand a new waiter a number below a live one), creates
  `ticket-<seq>.lock` and holds it with an exclusive OS lock for as long as it waits. A ticket is LIVE while its file
  is locked, so a waiter that dies drops out of the queue by itself; the next scan deletes its file. Scans run under
  `tickets.lock`, so a ticket is never probed between its creation and its lock. A waiter may take a free slot only
  when no live ticket is served before it, and it drops its ticket only AFTER it holds the slot; so a gate that
  finishes and re-gates at once queues BEHIND everyone already waiting, instead of winning every race by polling
  first. `gate-slot: waiting, k ahead (ticket t, cap N, <class>, …)` counts the live tickets served before one's own
  and is printed each time `k` changes; `gate-slot: took slot k of N (ticket t, waited s)` is printed on acquisition.
- **Two classes, landing first (kb/Work PB3103).** A ticket has a CLASS: `landing` (`ticket-<seq>-landing.lock`) for
  the gate `enqueue.py land` starts, which sets `COBOL_GATE_CLASS=landing` in its gate's environment, and
  `implementer` (`ticket-<seq>.lock`) for every other gate. THE SERVICE ORDER is every live landing ticket before
  every implementer ticket, ticket order within each class, so a landing's gate takes the next free slot ahead of
  the implementer gates already waiting instead of queueing behind up to the cap's worth of them (landing is the
  measure; owner 2026-10-12, "Supervisor lane (Recommended)"). The cap is the same for both classes, so the
  machine's total gate load does not change. An unknown class value stops the gate with the reason, as malformed
  settings do; `status` lists the queue in service order with each ticket's class.
- **Two pools: a light gate never waits for a full one (kb/Work PB3724).** The cap exists to ration builds and test legs,
  and a gate that runs neither queued for it anyway: on 2026-10-25 a notes-only gate (the INERT lane, §3.18), whose only
  slot work is about a minute of script self-tests in a WSL clone of HEAD, waited ten minutes behind the implementers' full
  gates. The slot directory therefore holds TWO POOLS (`gate_slot.Pool`, `GATE_POOL` and `LIGHT_POOL`), each with its own
  slot files, ticket queue (service order within it as above), cap, hold history and settings file (`light/`): the GATE
  pool is the one above, and the LIGHT pool's cap is the coded default `DEFAULT_LIGHT_SLOTS = 2` (an implementor's choice,
  not an owner decision: it adds at most two script-test runs beside the full gates), departed from by the `cap` key of
  `light/settings.json` (`gate_slot.py set-light-cap N [--until ISO]`; `clear light-cap` removes it). ⛔ A pool's settings
  live in its OWN directory, never as a new key of the gate pool's `settings.json`: every worktree reads that file with its
  own copy of `gate_slot.py` and refuses a key it does not know, so a key added there would stop every gate in every
  worktree not yet merged with main (the PB3724 review, R1-1; the self-test's light-pool arm pins it). `Host.take_slot(label, say, pool)` takes the
  pool its work follows: `Gate._inert` takes a LIGHT slot, the full lane's gate a GATE slot, and `gate_slot.py run --pool
  light|gate` wraps a command in either. No gate holds both, so neither pool waits on the other. `status` prints both
  pools. A waiting gate also says WHY it waits: a count that rises while it waits (`k ahead` 0, 1, 2, 0) is a landing
  ticket numbered after its own and served first (PB3103), and the log names each such ticket once; the gate next in line
  says it waits for the first released slot of its pool.
- **What holds it.** A Python caller holds a slot with `GateSlots.for_repo().acquire(label)` (a `Slot`, released by
  its `with` block or by the holder's death); a shell caller wraps a command with `gate_slot.py run [--label TEXT] --
  <command>`, which exits with the command's code. `run_gate_legs.py` takes the slot after the worktree's gate lock
  and its audits (kb/Work PB2524: the audits read only the tree, so they never hold a slot) and BEFORE the build
  (§3.14.3), and holds it through the population check, so the cap bounds the concurrent solution
  builds as well as the test legs. The slot is held by the gate's whole PROCESS TREE. On Windows, acquisition puts the
  holder ITSELF into a Job object with `JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE` before it spawns anything, so every
  descendant is in the job from birth, with no window in which a grandchild can escape, and if the driver dies the OS
  kills every `dotnet` process with it. On Linux the slot's descriptor is inherited by the children: Python's
  `subprocess` closes descriptors by default, so every spawn passes `**slot.spawn_kwargs()`. `dotnet` hands inherited
  descriptors on to its own children, and an `flock` lives until the last descriptor closes, so an ORPHANED Linux
  tree keeps its slot until it exits. The cap still bounds it; nothing kills it. ⛔ `spawn_kwargs` also turns
  PERSISTENT BUILD SERVERS off in every child's environment (`MSBUILDDISABLENODEREUSE=1`, `UseSharedCompilation=false`;
  MSBuild imports environment variables as properties): a reused MSBuild node or a VBCSCompiler server started inside
  the slot is a descendant of the holder that OTHER worktrees' builds connect to, so on Windows the job would kill it
  in the middle of another gate's build when this gate ends, and on Linux it would inherit the descriptor and hold
  the slot for its whole idle lifetime (a design correction made by M13, kb/Work PB1721). Acquisition is never nested beyond
  the one fixed order (worktree lock, then ticket, then slot), so no two gates can wait on each other in a cycle.
- **Who takes one.** Every gate (one shape, kb/Work PB3022), a landing's included, and the impact recorder. The
  BATTERY never takes a slot and never waits. N is ONE shared setting of the slot directory (kb/Work PB2514):
  `cobol-gate-slots/settings.json`, written only by `gate_slot.py set-cap N [--until ISO] [--why TEXT]` (under
  `settings.lock`, replaced atomically, recording who, when and why) and re-read by every waiter on every poll, so
  every gate applies the same cap at the same moment and a raise reaches the queue at once. While each gate read N
  from its own environment, gates started with different N shared one FIFO queue, and a cap-1 gate at its head
  waited for slot 1 while slot 3 stood free, blocking every cap-3 gate behind it (2026-10-07 16:13 PDT). A setting
  may carry an expiry (an ISO time with its UTC offset); once it passes, the CODED DEFAULT is in force again with no
  one acting, and `gate_slot.py clear [cap] [implementer-scope]` removes a setting (none named: both) so its default
  is in force at once — clearing both rewrites the file without reading it, the one repair of a malformed file.
  Malformed settings stop the gate with the reason. The same file carries the implementer SCOPE (`leg1`, batched
  gating, the default; or `whole`, both legs — §3.14.1), set with `gate_slot.py set-implementer-scope leg1|whole
  [--until ISO]`. ⛔ THE CODED DEFAULTS ARE THE OWNER'S DECISIONS (2026-10-10, kb/Work PB2514, PB2515), in
  `gate_slot.py` and nowhere else: `DEFAULT_SCOPE = "leg1"` and `DEFAULT_SLOTS = 3` ("Cap 3 permanently"). The cap's
  measurement (M2's acceptance: the largest N at which the lander's whole-Conformance leg stays within 1.25× of its
  quiet time with N implementer gates running, builds included; kb/Work PB1720, evidence `m13`: every gate cold, the
  lander at Normal, the implementers at BelowNormal, each rebuilding its compiler) gave quiet 143.1, 135.8 and 133.6 s;
  ONE implementer gate 1.24x and 1.30x; two 1.58x and 1.52x; three 1.56x — only N = 1 met the line. The owner chose
  three anyway: at cap 1 implementer gates waited 62 % of their wall time (the w1033 survey), and under batched gating
  every gate runs leg 1 only, so a landing's gate competes with three short gates rather than three whole
  populations. The verdict line prints the wait and the slot (`Slot.describe()`).
  `gate_slot.py status` prints the cap and the scope with who set them, EVERY held slot (one taken under an earlier,
  higher cap included) and the live tickets in queue order.
- `gate_slot.py --self-test` proves eleven arms against throwaway repositories, each with one linked worktree (the queue
  arms set cap 1 explicitly, so they prove FIFO, release and sharing on one slot whatever the default): the FIFO
  order (three waiters queued behind a holder are served in ticket order, and the holder re-gating the moment it
  releases is served LAST), the release on a killed holder, a dead waiter leaving the queue, the orphaned tree (on
  Windows the job kills the orphaned child and grandchild and the slot frees; on Linux the tree keeps the slot until it
  exits) and the sharing across worktrees (both checkouts resolve one slot directory, and a holder in one makes a
  waiter in the other wait); the SHARED CAP (a gate queued at the head under cap 1 and one behind it both take a slot
  the moment `set-cap 3` is written from the linked worktree, while the first holder still holds slot 1: no
  head-of-line block); the EXPIRY and the refusals (an expired cap 1 holds no gate back under the default 3, and an
  expired `whole` leaves the default `leg1`; an expiry already past or without its offset, a cap of 0 and an unknown
  scope are refused; either scope is accepted with or without an expiry; a malformed settings file stops a gate);
  STATUS (a slot held above a lowered cap is shown); the CODED DEFAULTS (with no settings file the cap is 3 — three
  gates hold a slot at once and a fourth queues — and the scope is `leg1`, and `status` says both are the default);
  CLEAR (one setting or both back to the defaults, a malformed file repaired by clearing both and refused by
  clearing one); and LANDING FIRST (two implementer and two landing gates queued interleaved behind one holder at cap
  1 are served landing, landing, implementer, implementer, `status` lists them in that order, and an unknown class
  value stops a gate). Each arm was seen RED on a planted defect: no ticket check, a ticket file counted live unlocked, no
  Job object, the descriptor withheld from Linux children, the per-worktree git dir, the cap read once per process
  (kb/Work PB2514's defect), `status` probing only the slots below the cap, and the defaults reverted to cap 1 and
  scope `whole` (the expiry, defaults and clear arms). It passes on
  Windows and under WSL, and the self-test runner (`scripts/self_tests.py`) runs it in every gate (reusing an unchanged PASS), the Linux
  gate and CI's Linux and Windows jobs, so each operating system's arm is proven where it runs; `GateSlotDriftTests`
  (Unit) holds the arms by name and the ALL GREEN count to them WITHOUT re-running it (kb/Work PB2882: its duplicate
  run under the test harness's fixed 120 s budget went red under a whole-population gate's load). Every helper process it starts exits once its sentinel
  file is deleted, so a red arm leaves nothing running on the host. ⛔ The cap does not span operating systems: a
  Windows lock and a WSL lock on a drvfs mount do not see each other; the repository's gates run on Windows
  (`build-local.ps1`), and the WSL legs of the Linux gate (section 3.15) are not counted against the cap.

**Retirement is measured** (owner, 2026-09-28): the cap goes when, over one full train, the median implementer
selection is under 25 % of the Conformance assembly, or a whole cold Conformance implementer gate at `BelowNormal` —
both legs, the leg barrier included — costs under 6 minutes on the shared host. Under ordering nothing is selected,
so the second criterion is the live one — and M6 and M7 are what can meet it.

#### 3.14.7 PB1712 under ordering

PB1712 found that the recorder cannot see static FIELD reads, so a change to one `DiagnosticCatalog` descriptor
reaches 80 tests in the map where the methods reporting it are reached by up to 9,225. Under selection that was a
silent SKIP of every test the map missed. Under ordering it cannot skip anything: a test the map under-reaches lands
in a later tier and still runs in the same gate, so the harm is a later red, never a missed one. The finding stays
documented in the note as an ORDERING-quality limit, and the gate needs no field probes for soundness. M11 deleted
the selection code (kb/Work PB1717), which closed PB1712, and M13 deleted `impacted_tests.py`'s filter line with
every caller (kb/Work PB1721), so a map recorded into the shared store can only reorder a gate, never narrow one.

#### 3.14.8 What the rejected SELECTION design's mechanisms become

`M1` (field probes, map schema 3) and `M9` (a map recorded per main commit) are not needed for SOUNDNESS. M9's
retirement (owner, 2026-09-28, kb/Work PB1709) is superseded by the owner's decision of
2026-10-24 (kb/Work PB3663): the loop records a map for every main commit it adopts, because batched gating (PB2515)
made leg 1 the only local run and the map's tiers now decide what it holds (§3.13). The gate still works with any map
or none, and on the one replay with named reds recorded cost carried the order (§3.13). `M3`, `M4`, `M5`, `M8` and `M10` (the registry restructuring) are shelved as a speed
measure (owner, 2026-09-28) and revisited on engineering merit only. `M2` (the cap), `M6` and `M7` are redesigned
above. The rejected design and both reviews are in the DEVLOG entry that pivoted it and in kb/Work PB1708.

#### 3.14.9 Implementation plan — gateable mechanisms, each within one implementer's turn cap

| id | mechanism | files | acceptance (measured, never asserted) | depends on | size |
|---|---|---|---|---|---|
| M6 | after warm-up the lexer performs no ATN simulation (§3.14.5); kb/Work PB1715 | `src/WiseOwl.Cobol.Frontend/Grammar/Core/CobolLexer.g4` and the predicate and action methods it calls; `tests/WiseOwl.Cobol.Tests.Unit/LexerDfaCacheDriftTests.cs`; this section | token streams of every suite source identical before/after (one-time differential, recorded in the DEVLOG); `LexerDfaCacheDriftTests` red on a planted predicate at the left edge AND on one placed after a common prefix's first character; zero ATN simulation on re-lexing the suite's ASCII sources (the one EOF transition per input ANTLR never caches is reported, not counted); the non-ASCII residual reported; `b2` at 12 threads ≥ 8× today's 1-thread wall, CPU utilisation ≥ 70 % (measured 55× and 21 %: the utilisation limit is now the workstation GC, §3.14.5); whole battery green | — | 140–200 turns |
| M7 | SR23 before the table-value fill; the fill bounded by the phrase (§3.14.5); defect note: id allocated by the orchestrator | `src/WiseOwl.Cobol.Compiler/Binding/Model/TableValuePlan.cs` (`TableValueOdometer.Resolve`), `DataBinder.ResolveTableValues`; battery summary top-5 report in `scripts/battery.sh` | the pb505 program rejects with `COBOL1946` in < 1 s through the CLI (was 29.6 s); both tests keep their assertions; every Format 2 VALUE golden unchanged; the battery summary lists the five slowest tests | — | 60–100 turns |
| M11 | the order plan: `NameKey`, tiers 0a/0u/1–3, the budgets and the collection cap (§3.13, §3.14.2); the NARROWING deleted — LANDED (kb/Work PB1717: b4 first red 0.32 %, b7 one tier-0u case and a 15.3 s floor; tier 0a corrected to ADDED test methods) | `scripts/spec/impacted_tests.py` (selection code DELETED — its filter line is always the whole-assembly filter until M13 deletes the line; `--plan` added), `scripts/gate_plan.py`, `tests/WiseOwl.Cobol.Tests.Unit/ImpactedTestsDriftTests.cs`, kb/Work PB1712 (closed) | `--self-test` covers every tier arm, `NameKey`, the unknown budget, the collection cap and the no-map / no-timings / stale-map / empty-leg-1 / whole-assembly-in-leg-1 arms; the filter line is the whole-assembly filter for a base WITH a map; `b4` re-run through the new script reproduces cheapest-first on 68b X (first red ≤ 1 % of the work); `b7` re-run through it: a golden appended to the 85 manifest leaves ONE tier-0u case and a leg-1 floor ≤ 16 s | — | 110–160 turns |
| M14 | ONE population check for every whole-assembly run, and the handshake scrub (§3.14.3–4) | `scripts/test_population.py` + `--self-test`; `scripts/battery.sh` (PHASE 1 population check; scrub); `.github/workflows/build-and-test.yml` (`conformance-population` runs the tool on the shard trx files; the inline `grep -c` block DELETED; scrub); `gen-vcr.ps1`, `gen-diagnostics-doc.ps1`, `scripts/spec/record_verdicts.py`, `scripts/spec/record_impact_map.py` (scrub); `GateLegDriftTests` (6); `docs/DRIFT_RULES.md`. As landed, the arm-(6) scan also found `build-local.ps1`/`.sh`, `guard.sh`, `guard-fast.sh`, `measure-battery-determinism.sh` and `filter_population.py` (scrubbed), the scrub grew the VSTest channel, the two other `--list-tests` parsers were folded into the tool's, and `FilterPopulationGuardDriftTests` recognises the new shard shape | the self-test's arms (short, over, skipped, definitions vs results); the battery's PHASE 1 prints each assembly's population line and is red on a planted dropped case; CI's guard red on a planted shard overlap that keeps the count; `b8`'s two inputs pass | — | 60–100 turns |
| M12 | the in-assembly leg filter, the handshake and the identity records (§3.14.3) — LANDED (kb/Work PB1719, evidence `m12`: no handshake, 35 / 29,715 / 9,319 definitions = `--list-tests`; a plan from `gate_plan.py` ran Characterization in one leg and Unit 28,754 + 961 and Conformance 5,069 + 4,250, each leg exactly its plan's cases, union = `--list-tests`, five identity records; eight partial or stale handshakes each `Failed!`, exit 1; a recording at its head recorded 39,100 tests, the gate's 9,319 + 29,746 + 35, with the watchdog clean) | `tests/_shared/GateLegs.cs` + `GateLegAudit.cs` (linked by the existing `_shared` glob, so no `.csproj` changes), `tests/Directory.Build.props` (the framework attribute, by assembly name), `tools/impact/ImpactTestFramework.cs` + `ImpactRecording.targets` (`IMPACT_RECORDING`), `tests/WiseOwl.Cobol.Tests.Unit/ParenTokenTwinDriftTests.cs` (repository-relative path argument), `GateLegDriftTests` (1), (2), (4), (5) in Unit and (4), (5) in Conformance and Characterization | with no handshake every assembly's count and verdict are unchanged; with a full one, each leg's trx definitions are exactly its leg's cases and the two legs' union equals `--list-tests`; each partial handshake (one variable, two, missing file, digest mismatch, bad leg) makes every case an execution error and the run RED; each leg writes its identity record; a planted non-permutation throws; no display name carries the repository root; a recording at HEAD still records every test (the recorder's watchdog) | M11 (the plan format) | 130–190 turns |
| M2 | the cross-worktree gate cap, FIFO (§3.14.6) — LANDED (kb/Work PB1720: the tool in train 71; N measured by M13's landing, evidence `m13`: one implementer gate 1.24x and 1.30x of the lander's quiet Conformance leg, two 1.58x and 1.52x, three 1.56x; the owner set `DEFAULT_SLOTS = 3` on 2026-10-10, kb/Work PB2514) | `scripts/gate_slot.py` + `--self-test`; `tests/WiseOwl.Cobol.Tests.Unit/GateSlotDriftTests.cs` | the self-test's five arms (FIFO order included), on Windows and on Linux; N measured: the lander's whole-Conformance leg ≤ 1.25× quiet with N implementer gates, their builds included | — | 90–130 turns |
| M13 | the ordered gate: driver, modes, worktree lock, run directory, fail-fast, and the wiring that deletes the selection interface (§3.14.1, §3.14.3–4) — LANDED (kb/Work PB1721, evidence `m13`: a real implementer gate GREEN with every population equal to `--list-tests`, 9,321 / 29,728 / 35; a planted leg-1 red stopped the gate after leg 1 11.7 s into the legs with 5,402 Conformance and 817 Unit cases named NOT RUN; a real lander-mode gate ran one leg with no slot; `check_practices.py` green with no `impacted_tests` pattern; the self-test's 22 arms each seen RED on a planted defect. Corrected on the way: a slot holder's children run with persistent build servers OFF (§3.14.6), the gate's audit list moved into the driver, and a defect in the driver still ends in one `NOT RUN` verdict line) | `scripts/run_gate_legs.py` (takes the slot, §3.14.6), `scripts/spec/record_impact_map.py` (the recorder takes a slot too, and spawns every child through `Slot.spawn_kwargs`), `scripts/build-local.ps1` + `.sh` (`-Filter` removed, a lander or implementer mode required (one shape since PB3022)\|implementer` required, `Leg` replaced); `scripts/spec/impacted_tests.py` (the filter line and `--plus` DELETED); `GateLegDriftTests` (3); in the SAME change every caller of either interface: `.claude/skills/workstream/templates/MANDATORY-PRACTICES.md` (I1, I2, I7), `implementer-brief.md`, `fix-lane-implementer-brief.md`, `dispatch-spec-implementer.md`, `lander-train-brief.md` and `lander-brief.md` (the lander mode), `.claude/skills/workstream/check_practices.py` (its required `impacted_tests\.py --base` patterns), `.claude/skills/workstream/SKILL.md`, `.claude/agents/cobol-implementer.md`, `.claude/skills/gate/SKILL.md`, `scripts/hooks/test_forbidden_commands.py` (its `-Filter` fixture), `tests/WiseOwl.Cobol.Tests.Unit/ImpactedTestsDriftTests.cs`, `docs/DRIFT_RULES.md`, `docs/DOC_INDEX.md`, plan §9, README, CONTRIBUTING, the PR template | the self-test's arms (§3.14.4 (3)); a real gate on a planted red in a leg-1 test stops after leg 1 with the remainder named; a real green gate's population equals `--list-tests` for all three assemblies; a real lander-mode gate runs one leg with no slot; `check_practices.py` green with no `impacted_tests` pattern left; over the next train, each implementer gate's time to first red, whole wall and green-path barrier cost (leg 1's wall beyond its share of the work, plus the extra host starts) are recorded, against the lander's single leg | M11, M12, M14, M2 | 170–220 turns |

M6, M7, M11, M14 and M2 are independent and may run in parallel groups; M12 follows M11; M13 lands last, with M2's N
in the same train so no implementer runs a whole-population gate uncapped. ⛔ **Nothing deletes an interface its
callers still read:** M11 only emptied the selection behind `impacted_tests.py`'s filter line (every caller kept
working, always handed the whole assembly), and the line, `--plus` and `-Filter` went in M13 together with every
caller named above. M13's landing updated CLAUDE.md "Testing" to the landed gate under the owner's standing
authorization (kb/Work PB1708, fifth decision, 2026-09-28). What M13's acceptance still records over the next train
— each implementer gate's time to first red, whole wall and green-path barrier cost against the lander's single leg —
every gate now writes into its run directory's `verdict.json` (`timings`, and each leg's per-assembly wall).

#### 3.14.10 The self-test phase: a PASS reused for unchanged inputs, one machine-wide budget, no verdict from the clock (kb/Work PB2914)

**The defect it removes.** Every implementer gate ran all of the ~68 script self-tests (§3.14.3 step 3) in a pool as
wide as the cores, AHEAD of its gate slot (kb/Work PB2524), and the runner judged a self-test RED after a fixed 900 s.
Nothing bounded the self-tests across gates, so a fleet's N gates ran N × 32 of them at once, the slowest
(`test_orchestrate_{stop,steer,breaker,handoff}.ps1`, `landing_check.py --self-test`) passed only after 15-20 minutes,
and on 2026-10-10 every one of 15+ implementer gates ended `SELF-TESTS RED … NO LEG RAN` on the clock alone: a test
that fails with no regression, which the owner forbids (2026-09-25), sitting inside the gate.

**1. Reuse — who records, who reuses.** `scripts/self_tests.py` TRACES every self-test it runs and records each PASS
with the content of everything it read, in ONE store shared by every checkout of the repository:
`<git common dir>/cobol-self-test-passes/<platform>/<test digest>/<deps digest>.json` (beside the gate slots, never
keyed on a branch name; the newest PASS_KEEP per self-test). `scripts/self_test_inputs.py` owns what "read" means:
- **traced** — the runner puts a `sitecustomize` tracer first on PYTHONPATH, so every Python process a self-test
  starts records, through the interpreter's audit hooks (PEP 578), each file it opens (a module's `__pycache__` file
  counts as its source), each directory it lists and each path it hands a child process, inside the repository;
- **static** — what no audit hook sees: the self-test's own file, the runner itself (`self_tests.py`,
  `self_test_inputs.py`: a change to how self-tests run re-runs them all), `# SELF-TEST-READS: <path> …` header
  declarations, and for a PowerShell or bash self-test the closure over the files its CODE mentions (comments
  dropped; a path resolved from the root, the mentioning file's directory or one above it; a directory only as a bare
  word beside the mentioning file, so `kb/Work` in a message string never pulls in the register);
- **git** — a `git` command a traced process runs inside the repository reads what `git_reads` says: nothing for its
  location, configuration, worktrees or a commit id; the NAMES (`ls-files`) or the CONTENT (`diff`, `ls-files -s`,
  `show <rev>:<path>`, any other command) under its pathspec, the whole tree when it names none.
A record holds each file's git blob id (the working tree's content id for an edited or untracked, not-ignored file; `-` where the run looked and found nothing) and a digest of what it saw
of each directory (`Tree.digest`: `entries`, `paths` or `content`); the blob ids are the index's, `git hash-object`'s
for a file the working tree edited, a submodule's commit for anything under it. `--reuse` reports a self-test REUSED,
unrun, while a record's inputs are ALL unchanged on the tree being gated. A self-test that RUNS says why on its
console line (kb/Work PB3061): `first run: no PASS recorded for <platform>, Python <v>`, or `re-run: N input(s)
changed since the newest PASS — a, b, c (+k more)`; the line also says how long it waited for a budget place when
that was a second or more, and the verdict line carries the budget's width and the total wait, so a gate log shows
whether the inputs or the budget set the phase's time (wave 1062: 50 of 75 re-ran in every first gate, the slowest
6 min, with no line saying what had changed). **Every gate reuses** (one shape, kb/Work
PB3022; `run_gate_legs.py` adds `SELF_TESTS_REUSE` to its SELF-TESTS audit, and a self-test that runs records its
PASS); **CI's `audits` and `windows-self-tests` jobs and the Linux gate never reuse**: they run every self-test, so a
branch that changes nothing a self-test reads reuses the PASS an earlier gate recorded, and the CI invariant (kb/Work PB1957) is
untouched — CI still runs them all. The `--built` phase reads build output no record covers: it always runs and never
records. Measured 2026-10-10 on this host: the whole phase recorded in 71-76 s; the same tree with `--reuse`, 0 ran and
69 reused in 0.9 s; a one-line edit of a `src/` file re-ran 2 (`test_dispatch_guard.py`, whose `git diff`/`ls-files -s`
read `src/` and `tests/`, and `test_plan_wave.py`, which opens 3,712 tracked files, `src/` among them) in 10 s; an edit
of one `kb/Work` note re-ran 4 in 10 s.
**What nothing sees**, and why it is safe: a PowerShell or bash process's own reads of a path it builds at run time
(declare it), the git commands such a process runs itself, `os.stat`-style probes (no audit event), and a self-test
whose Python children are started without the inherited PYTHONPATH (`-I`, a scrubbed environment). A PASS reused past
such a read is caught by CI's `audits` job, which never reuses — the same backstop batched gating relies on for
every gate's leg 2 (kb/Work PB2515, PB3022). A Python self-test whose trace does not show its
own file had no tracer, and records no PASS.

**2. The machine-wide budget.** A self-test waits for one of `BUDGET_CAP` places (one per core: the width one gate
alone always ran at) before it starts, and holds it to its end: one OS file lock per place under
`<git common dir>/cobol-self-test-slots/` (gate_slot.py's `ExclusiveLock`, so a dead holder's place is freed by the
OS). One thread per runner polls for a place; the rest wait without spinning. N gates therefore share the cores
instead of multiplying them, and a gate's self-test time no longer grows with N. ⚖ **Why a budget of its own and not
the gate slot:** the slot rations builds and test legs and is held for a whole gate (~4-6 min); moving the self-tests
inside it would make a red self-test wait behind up to three other gates' builds and legs, which is exactly what
PB2524 moved the audits out of the slot to stop (fail fast, kb/Work PB2523). A per-self-test place keeps fail-fast and
bounds the load, and with reuse a gate rarely needs a place at all. Every caller takes it.
⚖ **Why one place per core, and how a run says whether the cores were its limit (kb/Work PB3585).** The self-tests
mostly start processes, so a wider budget than the cores would help only if they spent the phase waiting rather than
computing. The GREEN verdict line says which, from every run: `the host's P processors u% busy` (`HostCpu`: two readings
of `GetSystemTimes` or `/proc/stat`, the reading `tests/_shared/HostLoad.cs` takes) across the phase. Near 100 % the
cores were the limit; well below it, `--budget N` measures a wider width for one run (a measurement only: no gate or CI
job passes it, since a run wider than another gate's also takes the places above that gate's width). Measured
2026-10-24 on CI's 4-core Windows runner (merge-group runs 40058976771, 40057441922, 40048830853): the phase took 290,
369 and 236 s for 1,031, 1,332 and 812 summed self-test seconds, with all four places busy from the 20th second until
the last long self-test, so the places were never short of work. The phase's own processes needed 988 CPU-seconds and
started 12,996 processes on this host (a Job object's accounting at 4 logical processors), 85 % of the 1,160
processor-seconds a 4-core runner has in 290 s. On a runner as fast as this host, a wider budget can win at most the
rest, and on a slower one less, so the default stays the cores. What does cost time is the order: a self-test starts in discovery order, so a long one discovered late
(`prune_worktrees.py`, started at 196 s of 286 in run 40058976771) runs alone for the last 50 s.

**3. No verdict from the clock.** A self-test is never RED for being slow. A run still going HANG_AFTER_S = 3600 s after
it took its place — counted from the place, so waiting behind other gates never counts; far above every PASS
measured (466 s beside four gates, 1,291 s at the worst of the unbounded fleet load the budget removed) — is stopped and
reported HANG with its output so far: its own state, `=== SELF-TESTS: HANG — … ===`, exit 3, and the gate's reason
`SELF-TESTS HANG`, never `SELF-TESTS RED`.

**Proof.** `self_tests.py --self-test` (`fleet_arms`) runs the phase from four CONCURRENT simulated gates over one
planted repository — each its own Runner, Budget and PassStore over the shared directories: a full run records; the
four gates on the unchanged tree re-run nothing; a changed input (a file read through a name built at run time, which
only the trace sees) re-runs exactly its self-test in every gate, at most the budget's two at a time; an edited import
re-runs only its importer; a planted 60-s self-test under a 1-s detector is HANG; a malformed store record is skipped.
Each arm was seen RED once with its mechanism disabled (reuse off: three arms; budget 99: the overlap arm; a hang
mapped to RED: the HANG arm). `self_test_inputs.py --self-test` proves the static rule, the git classification and the
tracer; `run_gate_legs.py --self-test` proves every gate reuses, and that a hung
self-test is `SELF-TESTS HANG`; `SelfTestDiscoveryDriftTests` holds the arms by name and forbids `--reuse` in CI and
the Linux gate.

**4. One scratch-repository fixture (kb/Work PB3584).** A self-test that needs a git repository builds and drives it
through `scripts/scratch_repo.py`'s `ScratchRepo` (`init`, `clone`, `origin`, `worktree`, `git`, `run`, `write`,
`commit`), the one place a self-test's git process starts. `init` and `clone` write the identity, no automatic
maintenance (`maintenance.auto = false`, `gc.auto = 0`) and no signing into the new repository's OWN config, so every
git process in it — the fixture's, the code under test's, a linked worktree's — behaves the same whoever runs the
self-test (the runner, whose private global config takes the same constants from the fixture, CI, or a hand run:
outside the runner the `prune_worktrees.py` self-test once started 74 background `git maintenance` processes in the
repository it was still building). Every command runs without git's repository-selection variables, a failed one
raises with git's own message (`run` is the unchecked form for a command whose failure is the case), and `isolate()`
drops the variables from a process whose code under test runs git itself. Until PB3584 fifteen self-tests created
their own repositories and seven carried a git helper of their own, each with its own identity and checking.
`scratch_repo.py --audit` reads every self-test's syntax tree (a function named for a self-test, a standalone
`test_*.py`): a `subprocess` call whose argv is a git argv, a repository created around the fixture, or a nested helper
over the subject's own git runner is a finding; `SelfTestFixtureDriftTests` runs the audit, adds a text net for
`"git"` and `"init"` in one argv, and holds the fixture's rules (the repository-selection variables, the identity, the
maintenance settings) to that one file.
**A self-test's scratch directory goes with it (kb/Work PB3194).** `scratch_dir(prefix)` makes it in the machine-wide
temp directory and registers `remove_tree` for the process's exit, so a return, a raised exception and `sys.exit` all
remove it; `remove_tree` makes a refused entry writable and removes it again, and prints what still cannot go. Before
it, three self-tests never removed their scratch and the rest called `shutil.rmtree(..., ignore_errors=True)`, which on
Windows cannot delete the read-only files git writes its objects as and says nothing: one host's temp directory held
7,359 such directories (0.75 GB of files) on 2026-10-23. The audit's SCRATCH arm finds a self-test's `tempfile.mkdtemp`
outside a directory it already owns, and any `rmtree(ignore_errors=True)` in self-test code.
**A C# test's scratch directory follows the same rules (kb/Work PB3194, review R1-2): `tests/_shared/ScratchDirectory.cs`.**
About a hundred test classes made a directory in the temp directory and removed it with `try { Directory.Delete(dir,
recursive: true); } catch { }`; the witness tests that loaded the program they compiled into the test host
(`LoadFromAssemblyPath`, `Assembly.LoadFrom`) kept its file mapped, so every one of their deletes failed on Windows, in
silence, and a test host the gate ends at its first red runs no cleanup at all: 16,096 `WiseOwlCobol_Tests_*` directories
in one host's temp directory on 2026-10-23 (17,331 old-layout `WiseOwlCobol_*` directories untouched for over 3 hours were
removed by hand when this landed). Now `ScratchDirectory.Create(prefix)` makes every scratch directory under ONE
directory per test host, `<temp>/wiseowl-cobol-tests/<pid>-<id>`, beside `<pid>-<id>.lock`, which the host holds exclusively
(`FileShare.None`, an flock on Linux) until its process ends; `Remove(dir)` deletes a directory whole, read-only entries
included, and reports what it cannot (`HarnessLog.ReportLeft`: standard error and `WISEOWL_COBOL_HARNESS_LOG`), never
throwing from a `finally` or a `Dispose`; the host's directory goes at exit, and every host's first `Create` REAPS each
host directory whose lock is free or gone, holding that lock while it removes the directory (deleted on close), so a
starting host can never lose its directory to a reaper. `LoadAssembly(path)` loads a compiled program from its bytes,
so the host keeps no mapping of the file. `FileHarness.TryDelete` (a test's physical file) reports through the same
`HarnessLog.ReportLeft`. `ScratchDirectoryDriftTests` holds every C# file under `tests/` to it (no recursive delete, no
temp directory of its own, no assembly load from a path, no delete in a `try` whose catches are all empty), with its
two exemptions named and each still checked live; `ScratchDirectoryTests` drives the read-only removal, the report of a
directory that cannot go (a held handle on Windows, an unsearchable directory on Linux), the reaping rules and the
unlocked load.

**5. One git process per question kind, never one per object (kb/Work PB3582).** A Windows process start costs tens of
milliseconds where Linux pays about one, so a script that asks git one question per branch, commit, pair or file is
bounded by process creation: merge-group run 40004159292 summed the script self-tests at 1,444 s on CI's Windows runner
and 144 s on Linux, and GIT_TRACE2 counted 3,048 git processes in `prune_worktrees.py`'s self-test, 1,471 in
`landing_check.py`'s, 646 in `enqueue.py`'s and 363 in `devlog.py`'s, nearly all one question about one object. The
same loops are the session probe's survey, the landing check and the planner's in-flight sets, so every session paid
it. `scripts/git_batch.py` answers a whole list from one process each: `Ancestry` (one `cat-file --batch-check` resolve
and one `git log --stdin <tips> --not <base>`: every commit the tips reach beyond origin/main with its parents, tree,
date and subject, so `on_base`, `beyond`, `is_ancestor`, the count ahead and the subjects are read from memory; a tip
that names no commit raises, never reads "on the base"), `diff_names` / `diff_patches` (one `diff-tree --stdin
--always` over `<commit> <parent>` pairs: one header per pair in input order, empty diffs included) and `object_ids`
(one `cat-file --batch-check` over `<rev>:<path>`), `tree_entries` / `blobs` (one `cat-file --batch` each: the files of many
`<rev>:<dir>` trees, then the content of the distinct object ids they name, so a note two commits share is read once, and `blobs` yields one blob at a time, so a caller that reduces each as
it arrives holds one;
`gen_ledger.measure_commits` measures every commit since the trend's last point from these two processes, kb/Work PB3315).
The survey reads every branch's tip and last commit from one
`for-each-ref` and every row's ancestry (MERGED, ahead, the review's coverage, `contained`, `carrying_findings`) from
one graph; the landing check charges each commit through the two batched diffs and reads `depends` from the graph; the
in-flight sets read each worktree with one `status` and every unmerged branch from one `for-each-ref --no-merged`;
the planner's classifier, carrier rule and `add_closed_notes` share one graph of the local branches. Each reader is
held to the one-process-per-object form it replaces by `git_batch.py --self-test`, and `GitBatchDriftTests` keeps the
per-object ancestry questions out of the survey, the landing check and the planner. Measured on the batched readers:
1,246, 846, 510 and 286 processes for the same four self-tests.

⛔ **A drift test holds a script self-test's arms from its SOURCE and never runs it a second time (kb/Work PB2882).**
The runner above is the one place a script self-test RUNS: in every gate, the Linux gate and CI, where a red arm
is a non-zero exit and a hang is bounded by the budget place, never by a clock. What the runner cannot see is an arm
deleted from a self-test, which still exits 0, so a Unit drift test names the arms it requires and
`SelfTestArmSource.AssertDeclares` finds each label among the script's string literals (implicitly concatenated
literals joined; an f-string arm named as its source spells it). A drift test that ran the self-test again did so under
the test harness's fixed 120 s per-process budget (`WISEOWL_COBOL_RUN_TIMEOUT_MS`), and under a lander's gate beside the WSL
Linux gate each went past it with every arm passing: `GateSlotDriftTests` (train 1055), then
`SelfTestDiscoveryDriftTests`, the landing lease's drift test (retired since, kb/Work PB3024) and `GateLegDriftTests`'
two driver tests (train 1058;
`self_tests.py --self-test` 19 s alone, over 120 s twice in that train's first gate).

**The case layout (kb/Work PB3726).** A converted script's self-test is a directory of case files under
`scripts/selftests/`, found by this runner's rule (b) with no change here; the layout, its collector and its drift test
are `DESIGN-orchestrator-loop.md` §16.2.

#### 3.14.11 The one Python interpreter the gate scripts run (kb/Work PB1637, PB3197)

**The rule.** A name on PATH is not an interpreter: on the Windows dev box `python3` is the Microsoft Store alias,
which runs, prints "Python was not found" and exits non-zero; a stock Ubuntu host has no `python`; and in the cloud
image a `python3` below the floor the scripts are written for (3.12) can stay first on PATH, where they fail to parse.
So every gate script runs ONE interpreter, resolved by PROBING each candidate, in order an explicit `$PYTHON`, then
`python3`, then `python`, by running it against the floor; nothing runnable is a loud refusal, never a skipped check.

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

**The drift test.** `PythonInterpreterDriftTests` (Unit, a tree-wide sweep): no bash script under `scripts/` and no
PowerShell twin of one names an interpreter as a command (`python3 x.py`, `command -v python`, `& python`); every script
that runs `$PY` sources its shell's resolver; and the two resolvers probe the same candidates in the same order with the
same floor probe. Before PB3197 the gate's own entry points bypassed the resolver (bare `python` in both
`build-local` twins; `command -v python3 || command -v python`, no probe and no floor, in `linux-gate.sh`), so
`bash scripts/build-local.sh` died `python: command not found` (exit 127, no verdict line) on a python3-only host.

#### 3.14.12 A gate outlives its caller, and a measured tree is gated once (kb/Work PB3536)

**The defect it removes.** A workflow agent's return kills the process TREE of every background command it started
(the harness walks the tree; the agent's shell is in no job object). Measured 2026-10-22 on wave 1150 group A: the
review finisher reached its turn cap while its gate ran, its return killed the gate mid-audit (run
`20261022T225440Z-2797b8`: no verdict, its log stopping in the audits), the wave read the return as SPLIT, and the only
path was a second finisher whose whole job was to re-run the same gate at the same head. Owner, ~16:50 PDT: "Reruning
these simply wastes tokens: but the workflow returned before its gate finished, so it needs the gate re-run at its
head and a round-2 review. - we should have the failure in the first place". The bar: the gate's result is PERSISTED
and AWAITED by the orchestrator, never left to an agent's turn budget.

**1. Detached, and followed.** `run_gate_legs.py` (what `build-local.ps1` and `.sh` run) makes the run directory and
starts the gate itself (`--run <run>`) through `scripts/detached.py`, THE one launch that outlives its caller: a
launcher that starts the target and exits at once, so no walk of the caller's tree reaches it (Windows: its own
process group and a console with no window, out of the caller's job where the job allows it; POSIX: its own session).
The landing lane starts its runners through the same function (§DESIGN-orchestrator-loop 4.9). The run directory
holds `gate.log` (the gate's whole output), `gate.pid` and, at the end, `verdict.json`. The caller FOLLOWS it: it
echoes the log as it grows and exits with the gate's own exit code once the verdict file is written, so every caller
that blocks on the `=== BUILD-LOCAL GATE: ` line (agents, `enqueue.py`, a person) sees exactly what it saw before. A
caller that is killed leaves the gate running to its verdict. The gate writes `gate.pid` itself, UNDER its worktree's gate lock (the launcher writes none), so a
pid file proves the gate took the lock. ⛔ **The window before the lock (kb/Work PB3707).** The run directory exists, and its
child is alive, for the moment of interpreter start and imports before the child takes the lock and writes `gate.pid`; neither
fact says a gate is coming. The launcher therefore records the child it spawned in `gate.spawn` (its pid), and the child's first
act under the lock replaces it with `gate.pid`. `run_gate_legs.gate_phase` reads a run by THREE facts, never a pid alone: the
gate lock is held by a gate writing this run (RUNNING); else the run has no `gate.pid`, is younger than `START_WINDOW_S` (60 s,
read from the stamp in its name, so no gap lies between the directory and its record) and its spawn record, when it has one,
names a live process (STARTING); else the gate DIED. A reused pid can therefore read a dead child as starting for the window and
no longer. `enqueue.tested_locally` reads a STARTING run as a verdict still owed (exit 12, GATE BUSY, never exit 14), and
`--await` reports it PENDING. To stop a gate, end the process `gate.pid` names.

**2. One way to wait: the verdict file.** `run_gate_legs.watch` — the CLI `run_gate_legs.py --await <run> [--worktree
W] [--wait-s N]` — reads the run directory, never a log's tail: the record's line and exit code once `verdict.json` is
there; `NOT RUN [verdict]` (exit 2, BRANCH) when the gate DIED without one (`gate_running`, over `gate_phase`: the worktree's gate
lock is not held by a gate writing that run and the run is not STARTING; never the pid's liveness, which the OS reuses; only the
follower watches its own freshly spawned child); PENDING (exit 4: exit 3
is BUSY, kb/Work PB3527) when the wait passes first; `NOT RUN [run]` (exit 2, ENVIRONMENT) when there is no such
directory. A relative `<run>` (`TestResults/build-local/<run>`, the form an agent returns) names a run of the worktree
that gated it, never of the reader's checkout: `run_gate_legs.resolve_run` is the one resolution, and the rolling
wave's await (`--worktree`, the agent's worktree) and `reports.Report.gate_state` (the report's Worktree field) both
read from the main checkout through it (review R1-1 of wave 1152). The follower, the rolling wave's await and
`reports.Report.gate_state` all read through `watch`.

**3. The handoff.** An implementer or finisher that reaches its TURN CAP while its gate still runs on its committed
head, with nothing uncommitted, returns status `GATE-PENDING` with `gate_run` and `head`, its report's header saying
`Status: GATE-PENDING` and `Gate run: <run>` (MANDATORY-PRACTICES P2's one exception). The rolling wave
(`wf_rolling_wave.js` `settleGate`) awaits the run inside the agent's own ceiling, one window at a time (one mechanical
`cobol-locator` per window runs `--await`; a workflow script has no process API and no clock): GREEN continues exactly
as a DONE return carrying that verdict (the finished test, then the branch review), any other verdict sends ONE gate
finisher (`gate_fix_rounds`), and no verdict by the last window, or the ceiling, is STALLED. The persisted verdict
also needs no awaiter: `reports.Report.finished` calls a GATE-PENDING report finished exactly when its run's verdict
is GREEN for the report's HEAD and measured that commit's own tree, unchanged while it ran; the planner waits on a
gate still running and plans a finisher for one that ended otherwise, and the branch survey reads the same predicate.

**4. Reuse: a measured tree is gated once.** Every verdict record (schema 4, `run_gate_legs.RECORD_SCHEMA`) carries the HEAD, the TREE the gate
measured at its start and again at its end (`Host.measured_tree`: the git tree id of the working tree — tracked files
as on disk, untracked files git does not ignore — written through a private copy of the index that keeps the index
file's own mtime, git's racy-git reference: an edit in the cached entry's own second keeps its size and mtime, and only
that reference makes git re-read such an entry by content; a path
`coord.NEVER_WORK` names measures at HEAD's content, so the harness's edit of the TRACKED `.claude/settings.local.json`
in every agent worktree is no difference and a GREEN at a clean commit measures as that commit's own tree, the tree
`gate_state` checks: review R1-2 of wave 1152), the base it planned
against and its exit code. A gate whose tree and base equal those of an earlier GREEN run in this worktree whose tree
did not change while it ran, under a scope that run covered (a `LEG 1 ONLY` GREEN covers `leg1`; a plain GREEN covers
both), takes the worktree's gate lock, answers from that record WITHOUT running an audit, a slot, a build or a leg, and
says so: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN — REUSED (kb/Work PB3536): tree … at head
… was gated in run <the measuring run> and has not changed since, so nothing ran (run <this run>); that gate: … ===`.
It is the self-test reuse rule (§3.14.10) extended to the gate, keyed on what was measured rather than on a branch or
a head: a RED is never reused, and the landing's gate on the merged head (another tree, another base) always runs.
A reused record carries its own seconds (`timings.wall_s`), so `landing_measure.py` counts what each gate cost.

**What it would have saved on 2026-10-22.** The wave journals of the day name one rerun of this shape (wave 1150 group
A, PB3527: a whole finisher dispatched to re-run the gate its predecessor's return killed, plus the second review
round it waited on); with the gate detached and handed over, the wave awaits the verdict and goes straight to round 2.

### 3.15 THE LOCAL LINUX GATE — CI's Linux legs under WSL before a push (kb/Work PB1732)

> ⛔ **Reshaped by §3.17.3 (owner 2026-10-11, kb/Work PB3015, Q1a and the 12:45 correction "Yes, both hosts, leg 1"):
> the WSL run becomes the LINUX HALF OF THE TARGETED LEG — leg 1 of the same plan in a WSL clone, in every local gate —
> and CI's whole Linux legs run only on the merge group.** `linux-gate.sh` keeps its whole-leg form as the tool that
> reproduces a Linux CI red on this box (`reference wsl_linux_repro`) and has the plan form, `--legs targeted` (kb/Work
> PB3612, §3.17.3). Below is the whole-leg gate.

CI runs most of its test jobs on `ubuntu-26.04` (pinned, so the runner image moves only when the repository says so),
and every local gate runs on the Windows host, so without this a
change's first Linux run was CI's. Train 71b showed the cost: a Windows path literal in a new drift test was green
on Windows and red in CI's Linux unit job. That was a ~30-minute round trip and a dropped cluster, for a failure a
2.5-minute WSL run reproduces.

**`scripts/linux-gate.sh`** runs, from any tree and any shell (`bash scripts/linux-gate.sh` at the tree's root: on a host that is not Linux it runs itself under WSL with `wsl.exe -d Ubuntu --cd <tree> --exec bash -lc ...`, its arguments verbatim, kb/Work PB2999),
the test projects CI's Linux jobs run, and the scripts they run: leg `guard` runs `bash scripts/guard-fast.sh`, CI's
`guard` job (the NIST suite through the `cobol` CLI, the manifest audit, the baseline check and the guard's own
self-tests), with a `TMPDIR` private to the clone because the guard writes fixed file names there. Leg `selftests`
runs always, beside the build: `scripts/self_tests.py` in the clone, as CI's `audits` job runs it; `selftests-built`
runs `self_tests.py --built` against the one build, beside the other legs (kb/Work PB2563, PB2879). Its first Linux run found two
self-tests that had never run on Linux: an unguarded Windows-only arm in `fleet_active_build.py` and
`test_autostart.ps1`, which installs a Windows Startup entry and is now marked windows-only.
`LinuxGateDriftTests` holds both sets equal to the workflow's: every `dotnet test` project and every `run: bash
scripts/….sh` step of a Linux job is a default leg. The guard leg was added 2026-10-04 (kb/Work PB1955, PB1957 row 40):
the guard job was the one CI Linux job no local gate ran, so train 1013's guard red on PB322's `TERMINATES` rows
reached CI behind a green Windows gate and a green Linux gate. It is a Linux-gate leg and not a `build-local` leg
because CI runs that job on Linux only; a Git Bash run on Windows would add minutes and no CI parity.

**How: a Linux clone of the commit, never the Windows tree.** The tree's COMMITTED HEAD is cloned into
`~/linux-gate/<tree>.<pid>/<tree>` on the Linux filesystem (removed when the gate ends, below). The clone is `--shared`: it borrows the Windows repository's object
store read-only, so nothing is copied. The solution is built there ONCE with the Linux SDK, and every leg then runs
in PARALLEL against that build, each to its own log (kb/Work PB2879: 234 s against 351 s with the legs one after
another), the shape of CI's ubuntu jobs. The git-ignored GnuCOBOL corpus is copied in from the tree, or fetched as CI
fetches it. Commit before running: uncommitted changes are counted, reported, and not tested.

**The gate leaves nothing behind on the Linux filesystem (kb/Work PB3194).** One built gate is about 1 GB and 80,000
inodes. The clone used to live at `~/linux-gate/<tree>` (with `<tree>.guard-tmp` beside it) and was removed only when
the same tree was gated again; worktrees come and go, so 345 clones filled the disk on 2026-10-15 (`No space left on
device` mid-leg read as 2,919 conformance failures, then the distro remounted read-only), 430 exhausted its inodes on
2026-10-19 and 394 again on 2026-10-22, and each time every Linux gate on the host ended NOT RUN until someone deleted
clones by hand. Now one mechanism in `linux-gate.sh` owns the footprint:
- each gate works in its own directory, `~/linux-gate/<tree>.<pid>/`: the clone (`<tree>`), the guard leg's scratch
  (`guard-tmp`, beside the clone and never inside it, kb/Work PB2989) and an `owner` file naming the gate's process by
  pid and start time, so a reused pid is never mistaken for it;
- the gate removes that directory on every exit path: the EXIT trap after its verdict (GREEN, RED, or a NOT RUN after
  the clone), and `on_signal` for HUP, INT and TERM, which first ends the legs' process trees (every background job and
  each process under it, read from `/proc` in one pass) so nothing writes into the clone while it goes. The one build
  runs in the background (`wait`) because bash defers a trap until a foreground command returns. What survives is what
  the gate already wrote to the tree's `TestResults/linux-gate`, the guard's forensics of a non-MATCH included
  (`GUARD_FORENSICS` points there);
- a gate killed outright (SIGKILL, or WSL ending its session) runs no trap, so every gate first REAPS: a directory
  whose owner no longer runs goes at once, and one with no owner file (the layout before this mechanism, from a
  branch not yet merged with it) goes when it is this tree's or older than 3 hours;
- then it measures the filesystem and refuses, `=== LINUX GATE: NOT RUN [disk] (…) ===`, below 1,000,000 free inodes
  or 20 GB free (about twelve built gates: the gate cap of 3 and every implementer gate beside it). `disk` is an
  ENVIRONMENT cause in `run_gate_legs.NOT_RUN_CAUSES`, so a landing retries it and never blames the branch. A
  filesystem that counts no inodes (btrfs) is held to the space bound alone; one `df` cannot measure is NOT RUN.
`linux-gate.sh --self-test` drives each exit path on real directories and processes (status 0, 1 and 2, and a TERM
mid-way with a background leg and its child, which must both be gone), the reaping rules and both bounds over a
stand-in `df`; removing the EXIT trap, the signal trap, the kill, the descendant walk, either bound, the liveness test
or the age rule each turns it red. The signal arm is SYNCHRONIZED, never timed (review R1-1: a 20 s watchdog failed a
slow correct gate and 5 s polls passed without testing): the leg's grandchild says READY through a fifo once it has
blocked TERM and the probe signal, the gate signals itself only after reading it, and the arm reads a second fifo the
grandchild holds open to its end, so it waits for that process's exit itself. A PROBE signal sent after the gate has
exited (SIGVTALRM) ends a grandchild the stop never reached. The grandchild takes the first signal with `sigwait` and
records TERM when TERM is that one or still pending beside it, so it records whether a TERM was ever sent, whatever the
order or the load; a PROBE alone is a red. A stop that does not end its jobs hangs the arm, which `self_tests.py`
reports HANG, a non-observation, never a pass. The grandchild was first a `bash -c` with a TERM trap, and that witness
was wrong: run 32 at a time, 2 of 256 runs recorded PROBE although `on_signal` had sent the TERM (status 0), because
bash did not run the TERM trap before the probe's trap ended the shell; the Linux gate's selftests leg at the landing
of c89014b6b failed so. The `sigwait` grandchild (Python, the one interpreter) is GREEN in 256 of 256 runs 32 at a
time, and the mutant whose stop signals only its jobs is red (`leg-child: PROBE`). The signal arm reads every process's `/proc` entry, which Cygwin's emulation on a
Windows host serves at about half a second each, so there it prints `n/a` and runs in the Linux gate's `selftests`
leg and CI's audits job.

Two earlier designs failed, and each failure was measured:
1. **Windows-built binaries run `--no-build`.** The Conformance tests find their goldens through `[CallerFilePath]`,
   which a Windows build bakes in as `E:\…` paths that are relative on Linux: 363 false reds.
2. **The Windows tree, with `GIT_DIR`/`GIT_WORK_TREE` exported** so Linux git could read a worktree whose `.git` file
   names an `E:/` gitdir. The variables reached every test process. `gate_slot.py`'s self-test (git init, commit,
   worktree add in its own temp repositories) then wrote into the REAL repository: `core.worktree` landed in the
   shared `.git/config` and broke git in every checkout until the owner removed it (2026-09-29).

So the script exports nothing. Its only git calls on the tree are READS (HEAD, the git directory, the count of
uncommitted changes, the tripwire below), all through one function, `wgit`: the TREE'S OWN git, without optional
locks. A tree on a Windows drive is read by Windows git (`git.exe`, through WSL interop) with its Windows path; a tree
on a Linux filesystem by Linux git. kb/Work PB2880 measured why: Linux git found the Windows-written index's stat data
foreign, re-hashed all ~16,000 tracked files across the 9P boundary for the one `status` (34 s at half of one core, the
gate's first serial valley in every run) and then wrote the refreshed index back into the real worktree, which Windows
git rewrote on its next command; `git.exe` reads it in 0.1-1.2 s. `LinuxGateDriftTests` holds every git call on the
tree to `wgit`, and every arm of it to `--no-optional-locks`.

Two guards stay beside the clone (group G, kb/Work PB1719). A self-test builds and drives its repositories through
the one scratch-repository fixture, `scripts/scratch_repo.py` (§3.14.10, kb/Work PB3584), which runs every git
command without the variables `git rev-parse --local-env-vars` names, and a self-test whose code under test runs git
in its own process drops them from that process first (`scratch_repo.isolate`), so an inherited `GIT_DIR` can never
redirect it into the real repository. And the script snapshots the real repository's `HEAD` and
`core.worktree` before the legs and is RED (`repository-written`) when either changed. (Not the worktree list:
other agents add and remove worktrees of the shared repository while it runs.)

**Line endings.** Every tracked text file is LF on every checkout (`.gitattributes` `* text=auto eol=lf`, kb/Work
PB3029, §3.17.4). Before it, `core.autocrlf=true` checked a script out CRLF in every fresh Windows worktree, and bash
died at its first line (`set: -: invalid option`).

**Verdict.** One `=== LINUX GATE: GREEN|RED|NOT RUN [<cause>] ===` line naming the HEAD it tested. NOT RUN is never
green; its cause's class is `run_gate_legs.NOT_RUN_CAUSES`' (§3.14.3 item 1).

**Which legs run (MANDATORY-PRACTICES I8, L10): all four, at every landing** — and at an implementer gate only while
the owner has set the implementer scope to `whole`; under batched gating (kb/Work PB2515, the default) the Linux gate
is the landing's, beside its gate, until the targeted leg runs on both hosts (§3.17.3, kb/Work PB3022 item 10).
- **Measured at `044d2aa4f`** (wave 1015 group V), 2026-10-04, `--nice`: unit 137 s, characterization 10 s,
  conformance 127 s, **guard 81 s** (364 NIST programs MATCH, audit clean, legacy Unit + Integration green). The
  guard leg adds about 1.4 minutes to a ~4.6-minute gate.
- **Measured at `4fdc6897c`** (train 71b with F), 2026-09-29: 195 s in all.
  - `unit`: 75 s including the build; exactly CI's one red, `Arm5`.
  - `characterization`: 8 s.
  - `conformance`: 102 s, for 9,321/9,321.
- **The shared `.git/config` and the worktree list were unchanged afterwards.**

That was about 3–4 minutes for the three test assemblies, about 6 with the guard leg. A platform-sensitivity detector, which would have added the
conformance leg only for flagged diffs, was built and deleted the same night. At that cost no selection is worth its
misses (the rule of section 3.14.1, ORDER, DON'T SKIP).

**Limits.**
- The local WSL distro is the same Ubuntu release as the pinned `ubuntu-26.04` runner (upgraded in place 2026-10-04),
  but it runs Microsoft's WSL kernel, not the runner's, and the gate runs Debug where CI also runs Release (kb/Work
  PB1957 lists every remaining difference). It catches platform assumptions, and CI's merge-group run remains
  the proof.
- The gate cap (section 3.14.6) does not span operating systems, so WSL legs are not counted against it.

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
- **Off switch: `CobolRuntimeReadyToRun=false`.** `tools/impact/ImpactRecording.targets` sets it, because the impact
  map's probes are woven into the portable runtime assembly (§3.13), which a recorded program must then load; the
  drift test's arms require the portable assembly under `IMPACT_RECORDING`.

**What the gate certifies.** The gates' programs load the Release ReadyToRun runtime, not the Debug build of it: the
same image the package ships. `src/WiseOwl.Cobol.Runtime` has no `Debug.Assert`, and CI's Windows Conformance shards
already ran Release, so no check is lost; the Debug portable assembly is still what the test hosts load in-process.

**Measured** (2026-10-07, this host shared with other gates; probe `E:\COBOL-coord\scratch\w1033x\startup.py`:
24 stride-sampled `tests/conformance/2023` programs, 21 runs each, the two runtimes interleaved run by run, 21 of 24
outputs equal to the golden in both arms):

| program start, median of per-program medians | Debug portable runtime (before) | ReadyToRun runtime (after) |
|---|---|---|
| run 1 | 104.7 ms | 74.5 ms (−29 %) |
| run 2 | 102.8 ms | 76.5 ms (−26 %) |
| quieter host, an earlier pair | 83.2 ms | 60.4 ms (−27 %) |

Build cost (probe `buildcost.py`: `dotnet build WiseOwl.Cobol.sln -c Debug` with node reuse and the compiler server off,
as the gate slot runs it, three rounds): a change to a runtime source costs 8.5–11.5 s with the images against
4.3–7.1 s without, about **+4 s** (two Release compiles and two crossgens, in parallel); a no-op build is unchanged
(2.3–4.6 s either way). A clean restore also downloads the crossgen2 pack and the two runtime packs once.

### 3.17 THE LANDING PIPELINE — a targeted local gate, CI as the regression net, one pull request per branch through GitHub's merge queue (kb/Work PB3015; owner decisions 2026-10-11)

#### 3.17.1 The decisions this section is built from

Measured on 2026-10-10 (frozen: `evidence/fleet-optimization/2026-10-11-pipeline-measurement-2026-10-10.md`): of 567
lander minutes, 233 (41 %) were local whole-population gate runs — 7–13 min on a quiet host, 25–53 min under load, 8
of 15 RED, mostly from fixed time limits under load — and 94 (16.5 %) were the former direct-push landing script holding the
landing lease while CI ran the same population a third time on 20 free runners in 13 min; 10 landers for 6 trains; first commit on
main 6 h 49 min after the day began. The owner, 2026-10-11:

- ~10:38: "We are free to completely rearchitect the build pipeline as needed to be maximally productive and minimally costly."
- ~11:20, the principle: "We need local testing to confirm the targeted changes are correct. The CI / CD should primarily
  ensure no regression on the remaining codebase. Another consideration, local runs are free." (The repository is PUBLIC,
  so GitHub-hosted standard runners are free for it; only larger runners or a private repository bill minutes.)
- ~11:30, Q1a: the lander's local gate becomes the TARGETED leg (its clusters' own tests, the likely reds, and the
  Windows file-system race witnesses that reproduce only on this box's ReFS drive); its local whole-population run and
  the WSL copy of CI's Linux legs are removed; CI stays the whole-population regression net on the pushed head.
- ~11:33, Q1b: landing goes through a **GitHub merge queue** on `main`.
- ~11:36, Q2: the DEVLOG becomes **one file per entry**, rendered as a view (the kb/Work pattern); rule 6 unchanged.
- ~11:40, Q3: **one pull request per finished branch**; the train and the lander role retire.
- ~12:45, the owner's correction, asked as "doesn't this mean many (all?) of the Linux only failures that we caught in
  WSL runs now aren't found until CI (very late)": **"Yes, both hosts, leg 1"** — the targeted leg runs natively on
  Windows AND under WSL in every local gate (implementer and enqueue); only the whole-population regression run is CI's.

The operator's constraint (2026-10-11 11:19): `E:` is ReFS and CI's Windows runner is NTFS; they delete differently (the
PB2917 race reproduced here in 20 of 7,200 probes and may not on NTFS), so the Windows file-system race witnesses run
locally at every enqueue and PB1957's local-vs-CI survey lists the filesystem.

#### 3.17.2 The contract — what every change goes through, in order

1. **Implement** (unchanged): an implementer in its worktree; its gate is the ordered leg 1 (§3.14, scope `leg1`), its
   report `Status: DONE` with the O11 header; it writes its own **DEVLOG entry file** (§3.17.4) with the number its
   dispatch spec allocated, and flips its notes.
2. **Review on the branch** (landed, PB3027; MANDATORY-PRACTICES I10): a read-only reviewer (`cobol-reviewer`, the
   `review` skill's "branch form": the full-code pass over `origin/main...HEAD`, the drift rules of the changed files,
   fixed time limits in tests, `cite.py --check`, the other dimensions where the diff warrants) runs THE MOMENT the
   branch is DONE, inside the wave's slot for that group (`wf_rolling_wave.js` `reviewed()`), a same-file predecessor's
   branch too: no branch waits for its successor, and a branch reviewed CLEAN is landable at once (kb/Work PB3381,
   below). Every decision is a line in
   `<scratch>/review-w<wave>-<letter>.jsonl`, appended through `scripts/orchestrator/review_findings.py`, which computes
   the verdict from the file and stamps it into the report's header — `**Review:** CLEAN <reviewer> <head>` or
   `FINDINGS <n> <reviewer> <head> <file>` — read back through `reports.parse_review`, the one reader. The stamp sits
   one line under the Status line, above an O11 header's `Branch:` line, and its reviewer label
   (`land-review-<branch>-r<k>`) carries the branch name, so `reports.parse_report` reads the branch, worktree and
   head from the header's own fields with the stamp lines skipped (kb/Work PB3077). FINDINGS → a
   REVIEW FINISHER (a fresh implementer, because a workflow cannot message an agent that has returned: it merges the
   branch, answers every finding by its id — fixed at its root, or `no_change_needed` with the reason — re-gates and
   returns DONE), reviewed again, at most `review_fix_rounds` (2) times; after that the FINDINGS stamp stays and
   `plan_wave.py` plans a finisher for it while the stamp covers the branch tip (a later commit that is not
   mechanical makes the branch WAITING FOR REVIEW to the survey and the planner alike: `prune_worktrees.stamp_covers`,
   kb/Work PB3394). **The rounds converge (kb/Work PB3381, owner 2026-10-18: "We should not be
   holding finished work this long, continually postponing it. Something in our process is flawed"; on the change:
   "Make the changes now and land the clean links as they come.").** Round 1 is the whole branch; round 2 and later
   review ONLY `git diff <the head the last round reviewed>..HEAD` (the wave passes `reviewed_head`) and the report's
   answers, since the earlier content is reviewed and is not re-opened unless the diff touches it. Every finding
   carries a severity (critical, high, medium, low, info) and the verdict is a threshold, `review_findings.py verdict
   --blocking`: critical, high and medium block in every round a finisher can still answer, critical and high only in
   the round after the last finisher; low and info never block; every finding stays recorded, and each non-blocking
   one is a LEAD the stamp lists under the report's `## Review findings` and the wave returns in `leads`, filed by the
   enqueue unit's clerk. Measured on wave 1118 (the PB3330 engine chain, 7 h, six groups DONE, none landed): PB3331's
   three whole-branch rounds found 5, 2 and 1 findings over 2.3 h, each new, and the cap turned one LOW finding into
   a DECISION row. **Nothing is held for a same-file successor (kb/Work PB3381):** the train-era rule that a
   predecessor "lands THROUGH" its successor saved nothing once every branch became one pull request in the merge
   queue (PB3023, PB3024) and kept a chain off main until its last link was clean; now the successor starts once the
   predecessor's review loop has ended, merges the REVIEWED head, and its pull request merges on top of the
   predecessor's. Unanswered findings have ONE source, the predecessor's findings file, and ONE reader,
   `review_findings.last_verdict` (its newest verdict: round, branch, head, verdict and the number of blocking
   findings), which the wave's `predecessorEnd` (through the CLI's `last-verdict`) and `prune_worktrees.py`
   `carrying_findings` (over `findings_files`: every `review-*.jsonl` in the coordination scratch and one level under
   it, and every file a stamp names, found by NAME only, never opened to decide what is one) both call. A report stamp is only their rendering: a review finisher rewrites the group's one report for its
   own branch with no Review line whatever it returns, so a hold read from the stamp released the successor exactly
   when nobody had answered them (branch review R3-1). While that last verdict is FINDINGS with blocking findings at a
   head a successor contains, the survey holds the successor IN FLIGHT (`waits_on`) until a later round of that review
   records CLEAN or the head is on main; a branch of the same note (its later finisher) is exempt. `predecessorEnd`
   reads `capped` when that verdict is the round after the last finisher (no finisher was sent to answer it: the
   successor neither answers nor re-records them) and `unanswered` otherwise (a finisher was sent and came back
   without answering, its report rewritten: the successor's review covers the predecessor's content and records again
   each finding that still holds; branch review R1-1, R2-1, R3-1). `capped` is one definition, `review_findings.capped`
   (the verdict blocked on `BLOCKING_AT_CAP`), and gives each case ONE owner (kb/Work PB3404): a capped verdict's
   findings are the predecessor's next-wave finisher's and hold the successor until it lands; an uncapped one's are
   the successor's, whose review answers them, so `carrying_findings` does not hold a successor reviewed CLEAN over a
   head that contains the verdict's head (`prune_worktrees.review_answers`), and no finisher fixes them twice. Every content problem is a TEAR, and a TORN
   findings file holds every branch it names (review R4-1, R5-1, R6-1, R6-2): the reader classifies a file as torn
   when it cannot be opened or read (gone, a directory in its place, a read denied), starts with a byte-order mark,
   holds bytes that are not UTF-8, does not end with a newline, holds no record, or holds a line that is not a record
   under `review_findings.SCHEMA` (every required field of its kind present with the right type, a verdict's FINDINGS
   when and only when its count is above zero; an extra field is allowed), the one record schema the writers share
   (`append` refuses to write a record that fails it). No verdict is read from a torn file, whatever its records say:
   `last_verdict` returns verdict TORN, `unanswered`, with `torn` naming the file, line, byte offset and reason
   (`reason` and `line` apart) and the `branches` and `heads` its parseable lines name (or the branch an enqueue
   unit's `review-<branch>.jsonl` names), and it never raises, so the CLI's `last-verdict` (exit 0) and the survey
   read one answer. `carrying_findings` holds each WAITING TO LAND branch it names and each one containing a head it
   names, the same note's included (a file that names none holds the branches of the note whose review it records,
   by the stamp that names it or its wave name), and writes the tear and what the operator does into the held row's
   `why`; `predecessorEnd` reads it `unanswered`, never `capped`; one file never stops the landing lane's tick; every
   writer refuses a torn file BEFORE writing, naming `review_findings.py repair <file>`, which only the operator runs:
   it removes a byte-order mark, keeps a last record that only lost its newline, removes any other torn tail, deletes
   a file left with no record, refuses a file it cannot read, and leaves an intact file unchanged.
   `review_findings.py --self-test` proves the rule by cutting a findings file at every byte offset, alone and with a
   record glued on, by each discovery case made at the file's first read (a read denied, a byte-order mark, a
   zero-byte file, a directory in its place, the CLI and the survey agreeing on each) and by each schema mutation;
   and a merged branch with no clean stamp at its tip never lands alone,
   it lands through the branch that carries it (`prune_worktrees.contained`). Only a branch stamped CLEAN at its tip is landable: the landing lane lands only those,
   and `prune_worktrees.py` lists any other DONE branch WAITING FOR REVIEW (a land unit dispatches the reviewer for
   one finished outside a wave). The landing step never reviews and never fixes, but it ENFORCES the
   stamp: `enqueue.py land` refuses (exit 11, UNREVIEWED) a branch whose newest report's CLEAN stamp names no commit of
   it, or after which it gained a commit that is not main's, a clean merge of main (its tree the auto-merge of its
   parents) or DEVLOG-only — a finisher's fix, a hand-resolved conflict — so that work is reviewed at its new tip first;
   the lanes I10 does not review (the registrar, the golden lane, the clerk: notes and spec-derived goldens) name
   themselves with `--lane`. The lander's former train review is retired. The role keeps the R2 design's 120-turn cap (one role, one cap; one branch's diff fits it).
   `review_findings.py summary` measures findings per branch, the re-review rate and reviewer minutes.
3. **Enqueue** (`scripts/orchestrator/enqueue.py land <branch>`, PB3023 — a SCRIPT, deterministic, no agent, started
   by the loop's landing lane the moment the branch is WAITING TO LAND, PB3103, DESIGN-orchestrator-loop.md §4.9;
   its gate's slot is served ahead of waiting implementer gates, §3.14.6): fetch;
   the review stamp (step 2; exit 11); MERGE
   `origin/main` into the branch — not a rebase, corrected at implementation: a branch that merged a same-file
   predecessor or a sibling carries conflict resolutions in its merge commits, which a rebase discards and asks for
   again, and the queue lands by merge commit anyway. **THROUGH THE MERGE DRIVERS** (`scripts/merge_drivers.py`, kb/Work
   PB3610): the files two landings both edit are merged by key, not by line — the keyed catalogues a landing adds an
   entry to (`wiseowl/diagnostics.json` and `options.json`, the traceability inventory, `constructs.json`), the work notes
   two branches append to, the runtime ABI pin (`RuntimeAbi.pin`, which `scripts/abi_pin.py` measures: the counters
   two branches each raised add, the merged surface's hashes are measured on the merged tree before the merge commit;
   that measuring build, like every build in a worktree after its agent has returned, is not blocked by a session's
   C# language server holding the source generator `WiseOwl.Cobol.Compiler.SourceGen.dll` from `bin\`: the generator
   project's `MoveAsideHeldOutput` target renames a held output aside before the copy, and
   `scripts/test_held_analyzer_output.py` rebuilds it with a real process holding it, kb/Work PB3674),
   and the public-API baselines (`src/**/PublicAPI.*.txt`, `cobol-keyed-publicapi`, kb/Work PB3727: a SET of symbol
   lines, so a line either side added is kept, a line either side removed is gone, the header stays first and the rest
   is sorted; the line union before it kept a removed line and once a symbol twice, PB3694).
   `DRIVERS` is the registry, `.gitattributes` repeats its patterns, `merge_drivers.py --check` and
   `MergeDriverDriftTests` hold the two together, and git is given the drivers on its command line
   (`-c merge.<name>.driver=…`), so no checkout carries configuration that can go stale, and the attributes with them
   (`-c attr.tree=…`, `merge_drivers.attributes_tree`: the merged-into side's `.gitattributes` with a `merge=<driver>`
   line per registry pattern after it), so a branch cut before a driver existed merges that driver's files through it.
   The other way round fails: until a branch that ADDS a driver lands, its landing merges main in through main's
   registry, which has no such driver, so that branch leaves the files the driver governs as main has them (a new driver
   lands alone, DESIGN-orchestrator-loop.md section 16.9). The review's coverage rule (`prune_worktrees.mechanical`)
   reads the same `is_drivers_merge`, built on `merged_tree` (kb/Work PB3713), the one definition of a clean merge's
   tree, which the branch survey's LANDED proof (`prune_worktrees.merges_to_main_unchanged`) reads too, with the first
   parent's attributes through `attributes_tree`; so a merge the drivers made is mechanical and a hand resolution is
   not. PROSE HAS NO DRIVER, ON PURPOSE (kb/Work PB3643): `docs/CONFORMANCE.md` and the `DESIGN-*.md`
   documents look append-only, but `merge_drivers.py --hunks <file>…` replays the merges in history and classifies each
   conflict hunk of git's line merge, and over `CONFORMANCE.md`, `DESIGN-runtime-library.md` and this document it counted
   45 conflicted merges holding 3 hunks where both sides inserted at one place and 115 where both REVISED text the base
   already held (one mechanism's paragraph, one Annex A.1 register row), so a union merge would keep a stale paragraph
   beside its replacement in a document that describes the current compiler, and a word-level merge came out different
   from the person's resolution in 3 of the 9 merges it resolved. Such a conflict is a decision about what the text now
   says, the finisher's; `--hunks` is rerun before any prose driver is proposed, and a file whose conflicts are
   insertions earns one. The landing's last line, `=== ENQUEUE: <OUTCOME> (<branch>) ===`, and the CONFLICT outcome
   inside it have ONE home, `scripts/verdict_line.py` (kb/Work PB3714): `enqueue.py` writes through `render` and
   `conflict_outcome`, the landing lane and the drivers' coverage report read through `parse`, `outcomes` and
   `outcome_files`, and `LandingLaneDriftTests` keeps every other script from spelling the prefix. What is left in
   conflict is real (a conflict stops it, exit 4: the merge is aborted, the branch's
   report gains `BLOCKED: enqueue … conflict in <files>`, a `cobol-finisher` resolves with `merge_drivers.py merge
   origin/main --keep`). A CLEAN merge that could not be COMPLETED is no conflict (kb/Work PB3673: a pin settle whose
   measuring build met a locked DLL used to end `CONFLICT in …RuntimeAbi.pin` and cost a finisher for a merge with
   nothing to resolve). `merge_drivers.merge` answers with a `Merged` (`clean`, `conflicts`, or `incomplete`: abi_pin's
   `Failure(cause, why)`, also for git's own refusal of the merge commit), and the cause is read from the gate's one
   classification, the `merge-*` rows of `run_gate_legs.NOT_RUN_CAUSES`: `merge-blocked` (ENVIRONMENT: a build output
   another process holds, MSB3027 or MSB3021, a full disk, no SDK, git refused a write) is exit 2, `MERGE NOT COMPLETED`,
   no report line, retried once by the lane and then the operator's; `merge-failed` (BRANCH: the merged tree's pin, build
   or measuring test) blocks like a red, exit 5, a finisher's. `merge_drivers.py merge` ends exit 3 with `NOT COMPLETED
   [<cause>] — …` where a conflict ends 1; with `--keep` a merge stopped at the pin stays in progress, for
   `abi_pin.py settle --stage` once the cause is removed. Before the worktree is first read, take its LAND LOCK (`<git dir>/cobol-land.lock`, released by
   every outcome; kb/Work PB3310): a second `land` of the branch is exit 13, never a red, nothing touched and no report
   line, and the landing lane retries it once the lock is free (two runs on one branch raced on the merge, the stamp and
   the gates' files); **READ THE RECORD OF THE BRANCH'S OWN LOCAL GATE, AND GATE NOTHING** (owner 2026-10-23 ~20:4x PDT, kb/Work PB3612: "Taking one
   hour to finish a branch is stupidly ridiculously slow … Developers merge separate branches into a continually moving
   main all the time without problems … Why don't we do that?", then "yes"; measured: PB3140's landing ran 3 local gate
   runs, 4,615 s, because main moved twice while it waited for a slot; five branches took about an hour each from CLEAN to
   queued; the lane lands 1.47 changes per hour). A branch is gated locally ONCE, at its head, by its implementer or
   finisher: that run (§3.17.3: leg 1 on Windows, then the same plan's legs in a WSL clone of the committed head, under the
   one slot) is the whole definition of "tested locally". `enqueue.tested_locally` reads it from the worktree's newest
   `TestResults/build-local/*/verdict.json` runs and refuses the tip no run covers (exit 14, UNTESTED, before anything is
   merged; the report gains `BLOCKED: enqueue — no local gate is recorded GREEN …` and a finisher gates the tip): a run
   covers it when it read GREEN with exit 0, measured the COMMITTED head's own tree (`tree` equals `head_tree`), carries a
   GREEN Linux half of that tree on a host that runs one (the inert lane's is the script self-tests alone, §3.18), and its head IS the tip or every later
   commit is main's, a clean merge of main or DEVLOG-only (`prune_worktrees.unreviewed_since`, the review's coverage rule).
   The record is written by the BRANCH's gate driver and read by main's lander, so a record the driver wrote before a
   field the lander requires existed (`run_gate_legs.LANDING_FIELDS`: `head_tree`, `linux`; schema below
   `LANDABLE_SCHEMA`) is its own reason, "written by a gate older than this lander" (`run_gate_legs.predates_landing`),
   never "uncommitted changes", and its BLOCKED remedy is to merge origin/main into the branch FIRST and gate the merged
   head: gating the tip again with the same older driver writes the same record (kb/Work PB3660). Whenever the lander
   starts to require a new field: raise `RECORD_SCHEMA` and `LANDABLE_SCHEMA` and add it to `LANDING_FIELDS`
   (schema 3 was written both before and after PB3612 added the two fields without a bump, so for it the fields are the
   discriminator). A later RED does not unrecord an earlier GREEN of the same head. The merge with main is the MERGE QUEUE'S to test
   (step 4): `ci-gate` runs the whole population on the exact merge, both operating systems, and main moving again
   costs no re-gate. ONE exception, the case a developer would also re-test: when bringing main in changed a file the
   branch also changes (`enqueue.merge_overlap`: a true textual merge in a shared file; the files a merge driver
   merges by key and the inert ones no test reads are set aside), the one gate (`build-local`, Normal priority, its
   slot served first) runs once on the merged head. The lanes I10 does not review write no gated report, so with no
   record the landing gates their tip once itself. That one gate's verdicts are read as before: exit 5 on a red,
   the host named; exit 12, never a red, when the gate driver says BUSY (another gate holds the worktree's gate lock,
   nothing ran, and the landing lane retries it once the lock is free, kb/Work PB3527; the same exit when the gate's Linux half found a Linux gate of the tree running (`linux-gate.sh` BUSY: its message names the running gate's pid and start time, and `linux-gate.sh --probe` is what the lane's wait asks, kb/Work PB3649); the same exit, for the same wait, when the
   worktree's own gate is still running (or STARTING: spawned moments ago, its child alive, the lock not yet taken, kb/Work PB3707) and has written no verdict, `run_gate_legs.gate_phase` over `live_gate` (the gate lock held; never a process id, which Windows reuses), kb/Work PB3662, where the record would
   otherwise read as exit 14, UNTESTED, and stop the branch); exit 15, never a red, in step 4 when the branch merged a predecessor
   whose pull request is still open into main and so adds that pull request's stamped DEVLOG entry beside its own
   (`enqueue.open_predecessors`: nothing queued, and the lane retries it once no open pull request owns an entry the
   branch carries, kb/Work PB3145); exit 16, never a red, in step 4 (4a) when an open pull request changes a file a
   merge driver covers that the branch changes too and ENTERS the queue (`enqueue.driver_file_holders`, counting
   `queue_facts.PullRequest.entering`: in the queue or with auto-merge on, not bounced, conflicting or red; a bounced
   one holds nothing, it re-enters only through a fresh `land`; GitHub's merge queue builds its
   merge groups with plain git and none of `merge_drivers.DRIVERS`, so the two would conflict inside the queue; nothing
   is stamped or queued, and the lane retries it once no open pull request shares such a file, main then coming in
   through the drivers; the repository's queue-entry lock is held from that look to the queueing, so two landings of
   one file cannot both look and both queue, kb/Work PB3642); a branch whose tip is already inside origin/main (its
   pull request merged) ends exit 0 NOTHING TO LAND, never exit 6 (kb/Work PB3157); a gate NOT RUN, which measured
   nothing, by its cause's class, kb/Work PB3526: exit 2, no red, for an ENVIRONMENT cause or a BRANCH cause main fails
   the same way at the same step, else exit 5 like a red. Bring main in AGAIN when it moved while the pull request is
   queued, without a gate (review R1-2, corrected at implementation: the queue merges other pull requests meanwhile,
   `queue` takes only a head that descends from main, and re-gating every such head would race the queue forever; the
   merge group below integrates the branch on the current main and runs the whole population; a conflict here is exit 4
   too);
   `devlog.py stamp` numbers the entry (once: a stamped entry keeps its number, PB3123); run the landing checks that used to live in
   the former direct-push landing script — `devlog.py check` (exactly one new entry file, stamped and well-formed), `work.py check` (the register audit, run by the merged
   head's own `scripts/spec/work.py`: a note main gained after the branch's gate meets a rule the branch added there, never first in the
   merge group's CI, PB3389), `rename_diagnostic_prefix.py --check` (the DIAGNOSTIC PREFIX audit, run by the merged head's own copy for the same
   reason: a branch cut before the codes became `COBOL####` that merges main cleanly meets it here, never first in CI's `audits` job, PB3575),
   `rename_retired_names.py --check` (the RETIRED NAMES audit, the same shape for the same reason: a branch cut before the code took the
   product's name, WiseOwl.Cobol, PB3837)
   and `landing_check.py` (a path no landing carries, `coord.NEVER_WORK`, then the R3 file-set
   partition, PB2118) — then `gh pr create` (title = the branch's one-line subject, body = the forensic message) and
   `gh pr merge --merge-queue`-equivalent (`gh pr merge --auto`) so the PR joins the queue. Done. No lease, no local
   whole population, no waiting for CI. That the pull request then ENTERS the queue is the landing lane's QUEUE WATCH
   (DESIGN-orchestrator-loop.md section 4.9 step 4, kb/Work PB3527): on every tick it reads the merge queue's entries
   and the head's required check-runs (`queue_facts.py`) and, past a 10-minute grace, re-runs a stuck check, then
   closes, reopens and re-queues the pull request, then asks the operator. A HOST-I-O group (a file of its set under
   `src/WiseOwl.Cobol.Runtime/IO/`) runs the Linux unit and conformance legs from WSL at its own gate (MANDATORY-PRACTICES I8;
   `make_dispatch_specs.host_io`, kb/Work PB3381: wave 1118's PB3333 was refused at its landing by 31 NIST RL programs that
   differ on Linux, 2.6 h after its DONE), and CI's merge group runs all of them. Every gate row (the rare gate above) hands
   its gate to `run_gate_legs.py`, which takes the slot, and `enqueue.py` runs no row while one would not (PB3104; the
   Linux row, which ran under `gate_slot.py run` as a landing ticket, is gone with the landing's Linux gate).
4. **The merge queue** (PB3024): GitHub builds the merge group (several queued PRs on top of `main`), the workflow runs on
   the `merge_group` event — the full matrix: Linux and Windows, the sharded conformance legs, unit + characterization,
   the population check (§3.14.4), audits, guard, INV-1 — `ci-gate` stays the one required check, and main advances in
   order. A red bounces only the PR that caused it (GitHub re-tests the rest); the loop sees the bounce
   (`gh pr view --json mergeQueueEntry,statusCheckRollup`), names the CI red in the branch's report and dispatches a
   finisher; the fixed branch is reviewed again at its new tip (step 2), then enqueued again.
   **What the queue stacks (kb/Work PB3107, PB3123; owner 2026-10-13: "Why are many, many github runs failing? Due
   to our local testing, we should rarely see failures", then "Fix them now").** A merge group is built ON TOP of every
   group queued ahead of it, in the order GitHub picks when each pull request's own checks finish, so the tree a
   group tests is one no branch ever checked: main, then each pull request ahead, then this one. Measured
   2026-10-12 23:46Z to 2026-10-13 07:06Z: 12 of 39 merge-group runs failed, nine on the arch-oracle baseline (two or
   three manifests in the stacked tree: each branch had re-recorded its own on one main, and PR 29, which changed no
   oracle input, failed only for being queued behind two that did) and two on the DEVLOG number (an entry stamped 1990
   queued behind one stamped 1991). THE RULE that follows, for every check a landing or a merge group runs: **a
   check's verdict on a pull request may not depend on what else is queued.** A check that holds a branch to a
   committed record of its own tree fails it, because no such record describes the stacked tree; so does one that
   orders landings by a value fixed before the queue orders them. The two such checks were redesigned, never relaxed
   into a retry: (a) the oracle commits nothing: its baseline is the capture of a change's own base commit, made on
   demand by `compare_oracle.py` (a capture is a pure function of its commit, DESIGN-architecture-review.md §4.1), and
   the landing oracle and its exit 7 are retired with the committed manifest; recording on the merged head at enqueue
   (the note's candidate) was rejected because two pull requests queued together still stack two such records;
   (b) the DEVLOG number is STAMP order, not merge order: `devlog.py check` holds a new entry above every entry of the
   tree it was STAMPED on (the first parent of the commit that added its file) and unique on the landing's tree,
   never above the group's base; serializing the auto-merge so queue order equals stamp order was rejected because a
   bounced pull request re-enters behind later ones. Proof: `devlog.py --self-test` builds the stacked trees as the
   queue does (a merge commit of each branch onto the group ahead) in both orders, and `LandingPipelineDriftTests`
   keeps the oracle's record and the landing oracle out of the tree. The checks swept for the same dependence and
   found free of it: the witness-loss audit and the `changes` job's docs-only decision both read base..head, and a
   group's `base_sha` is the head of the group queued ahead of it (run 38360587401's base held entry 1996 before that
   pull request reached main), so the range is this pull request's own change whatever is queued with it.
   `landing_check.py` reads the in-flight branches on purpose, and only locally.
   **Superseded runs are cancelled (kb/Work PB3769; owner 2026-10-25: "cancelling superseded runs automatically").**
   When an entry ahead fails or leaves, GitHub rebuilds every group behind it: each entry gets a new merge-group commit
   and a new run, and the old runs kept running, holding runners under the concurrent-job limit (2026-10-25: after #591
   was ejected at 20:18Z, runs 40228927401 and 40228888256, later 40230142896, 40230142122 and 40230639741, held
   runners until cancelled by hand). A `concurrency` group cannot catch them, since each group's ref
   (`gh-readonly-queue/main/pr-<n>-<base sha>`) is unique. So every merge-group run starts with the `supersede` job (no
   `needs`): `scripts/orchestrator/cancel_superseded.py` lists the unfinished merge-group runs, THEN reads the head
   commit of every current queue entry (`queue_facts.live_heads`, the one reader of the queue's facts; in that order,
   because a run created after the read would otherwise look superseded), and cancels each run of the queue on main
   whose head commit no entry heads, never its own run and never a live entry's. The replacement groups' runs are
   created at the moment of supersession, so they are what sweeps the runs they replaced. ⛔ It cancels NOTHING when
   the queue cannot be read whole (a failed or malformed read, no queue, more entries than one page), reads empty, or
   does not hold the run's own head commit (a read taken before GitHub recorded the group, or the run is itself
   superseded); each is a warning annotation. Every outcome exits 0 and EVERY step of the job is `continue-on-error`
   (the checkout too, kb/Work PB3820: the script step runs only when `steps.checkout.outcome == 'success'`, and a failed
   checkout is said as a warning annotation by a step of its own): housekeeping never decides a verdict, and the job
   stays a `needs` of `ci-gate` because the drift check above wires every job. A cancelled superseded run fails its own `ci-gate`, on a commit no entry heads any more,
   which the queue no longer reads. ⛔ The job checks out only `scripts/orchestrator` and `scripts/standard_streams.py`
   (a sparse checkout) and its script step is `continue-on-error`, so a module its scripts import that the list leaves out
   would make it cancel nothing while the run stays green (kb/Work PB3637 added `standard_streams.py` by hand):
   `SparseCheckoutDriftTests` (kb/Work PB3787) reads the closure of every sparse-checkout job of the workflow and
   refuses a file it runs or imports that the list does not cover (§3.17.8).
5. **On `main`**: the merged commit is the tested merge-group tree. The `push` run keeps only `changes` + `ci-gate` (the
   record the probe reads); the matrix runs on `merge_group` and `workflow_dispatch`. On `pull_request` only the cheap
   jobs run (audits, the DEVLOG and work-register checks), so twenty PRs do not queue eighty matrix jobs behind the
   20-concurrent-job limit of a public repository.

The implementer's leg-1 gate stays first for every change (the owner's principle); CI is the regression net and the
only whole-population run.

#### 3.17.3 The gate driver's ONE mode (PB3022)

- **LANDED (kb/Work PB3022):** the lander's mode of `build-local` and its arm of `run_gate_legs.py` are deleted: the gate has one shape, the
  ordered leg 1 of §3.14 (a plan, fail-fast, a gate slot). The scope `whole` stays as the owner's on-demand switch
  (`gate_slot.py set-implementer-scope whole`) and the battery's form; it is nobody's per-change gate.
- **LANDED (kb/Work PB3612) — the targeted leg runs on BOTH hosts, inside the implementer's gate** (owner 2026-10-11 ~12:45, after asking whether dropping the WSL gate would
  push every Linux-only failure to CI: "Yes, both hosts, leg 1"): the one gate runs leg 1 natively on Windows and then,
  under the same gate slot, the SAME plan's legs in a WSL clone of the committed head: `run_gate_legs.py`
  (`Gate._linux_leg`, `Host.linux_leg`) starts `bash scripts/linux-gate.sh --legs targeted --plan <run>/plan.json
  --plan-legs <n>`, which clones HEAD, builds it once and runs the driver's own `--plan-leg` there (`plan_leg`: the legs of
  an existing plan through `Gate._run_leg`, the population check and the identity check of this clone's listing and
  binaries; no audits, no build, no slot of its own, and a case the plan never heard of runs in leg 1). It runs in the
  IMPLEMENTER'S gate and nowhere else (the landing reads that gate's record: §3.17.2 step 3, owner 2026-10-23). The verdict
  file records the Linux half (`linux`: verdict, cause, the committed `tree` it measured) and `head_tree`; a reused
  answer needs a GREEN Linux half of this head's tree (the inert lane's: the script self-tests, §3.18); a Windows half that is not green never
  starts the clone; a Linux half that measured nothing ends the gate NOT RUN with the Linux gate's own cause, and a BUSY one (another Linux gate of the worktree runs: linux-gate.sh runs one gate per tree, kb/Work PB3310) ends it BUSY, never RED. Measured why: on 2026-10-10 three of 38 clusters had Linux-only reds, all in the clusters' OWN
  tests (a locale golden's Linux text, a lock-race fix red 10 of 10 on Linux, a host-path length test), and the WSL
  run caught each before a CI round trip; a leg-1 run under WSL costs the clone and build (~1–2 min) plus the leg,
  not the 5–10 min of CI's whole Linux legs. Only the whole-population regression run is CI's. Fail-fast order:
  Windows first (no clone), Linux second; a Linux-only red names its host in the verdict line.
- **LANDED (kb/Work PB3022) — tier 0h, host-only witnesses, and tier 0c, the cheap tree-wide sweeps** (§3.14.2's
  tiers; `TIER_ORDER = 0a, 0h, 0c, 0d, 0u, 1, 2, 3`, tier 0d since kb/Work PB3531; the race witnesses are the Unit classes that drive the sharing
  layer's host types from concurrent workers, the sweeps are `drift_rules.py --sweeps`). The design as decided: every
  test case whose class carries the xUnit trait `Host` with a value naming a property CI's runners lack (`ReFS` today)
  is in leg 1 at every gate, whatever the diff. Membership is DERIVED from the trait, never listed; a drift test asserts
  that every test class under the file-system race witnesses (`SidecarColumnLockHostDriftTests`, `RegionLocksDriftTests`,
  their siblings found by `drift_rules.py`) carries it. CI runs the same tests on NTFS, where they may not reproduce.
- **Audits only when their inputs changed, at every gate** (PB2914 generalized from the implementer gate to the one
  gate): a script self-test, a drift-rules regeneration or a citation check with inputs unchanged since a recorded PASS
  is reused, never re-run; CI's `audits` job (53 s, its own runner) runs them all on every merge group.
- **No fixed time limit in any test** (owner rule 2026-09-25) — **LANDED (kb/Work PB3022)**: the eight Unit drift
  tests that re-ran a script self-test under the harness's fixed 120 s budget (`GateSlotDriftTests`'s siblings) hold its
  arms from source (`SelfTestArmSource`, the PB2882 mechanism) and leave the run to `self_tests.py`; the seven regex
  match timeouts became `RegexOptions.NonBacktracking` (linear, so no clock); the two generators' bounded
  `WaitForExit(n)` after their streams' EOF became the unbounded wait (and left `ProcessObservationDriftTests`'
  exemptions); the interpreter probes take the harness budget. `NoWallClockAssertionDriftTests.NoTestSetsALiteralTimeLimit`
  fails on a new literal limit in any shape. What remains is the ONE harness budget rule (`ObservationBudget`,
  kb/Work PB3108: one 120 s unit, `WISEOWL_COBOL_RUN_TIMEOUT_MS`, per serial step of the work the process was started to
  do), a hang guard whose expiry is a named non-observation, never a value.

#### 3.17.4 Conflict-free landing files — the tree holds SOURCES, views are RENDERED (PB3025, PB3026)

Two queued PRs that both insert at the top of one file collide, and the second is bounced; the queue is only as parallel
as the files are disjoint. The rule: a landing adds or edits files that are ITS OWN; anything derived is rendered on
demand and not committed.

- **DEVLOG (PB3025).** `DEVLOG/NNNN-<slug>.md`, one file per landing, header line unchanged (`## Entry NNNN —
  YYYY-MM-DD HH:MM TZ — Title`). The implementer writes the entry WITHOUT a number, as `DEVLOG/pending-<slug>.md`; the
  number is stamped AT ENQUEUE (`devlog.py stamp`, called by `enqueue.py`: `alloc.py devlog` allocates `NNNN`, the file
  is renamed and its header completed, one commit on the branch before the pull request opens), so number order IS
  STAMP order (operator review item 2; gaps are fine). It is not merge order, and the check never asks it to be
  (kb/Work PB3123, §3.17.2 step 4): GitHub orders the queue after the stamps, so two landings stamped minutes apart
  can reach main in the other order, and a bounced pull request keeps its number when it lands later; "newest first"
  by number is newest first by stamp. `python scripts/devlog.py render [--since NNNN] [--out <file>]`
  prints the descending narrative (`DEVLOG/README.md` first, then the entries newest first, the files concatenated
  byte for byte; `DEVLOG.rendered.md` is gitignored); `devlog.py check` is the landing rule `landing_devlog.py` was
  (exactly one new, stamped entry file in the PR — new meaning a header the base's history lacks — numbered above
  every number of the tree it was stamped on, header well-formed; a `pending-` file, a stray file, an entry whose header disagrees with
  its name, two files with one number, the monolith back, or a deleted entry is a red — a deleted entry counts the
  base's monolith too, header by header, so the one landing that migrates the log cannot drop an entry main has), run
  by the landing checks (`enqueue.py checks`, run by `enqueue.py land`) and by CI's `audits` job on every pull request and merge group. Its tree half (`lint`) also runs
  over the repository's own `DEVLOG/` in `devlog.py --self-test`, so every gate holds the whole log to it. **Line endings (PB3029; owner 2026-10-11 ~12:35: "standardize on a single line-ending
  format"):** LF everywhere. `.gitattributes` opens with `* text=auto eol=lf`, so git stores and checks out every text
  file LF on every OS; a file that must keep its bytes is declared `-text` there with its reason. One normalization
  landing made every blob LF (the index was already LF for 15,234 files: what changed is what each OS's WORKING TREE
  shows). `scripts/line_endings.py --check` — a gate audit, CI's `audits` job and a `windows-self-tests` step —
  refuses a CR in a tracked text file's blob or checkout and a Python text-mode write that does not state `newline=`;
  `--fix` rewrites a checkout made before the rule and then re-states the index entry of every file `git diff-files`
  lists whose cleaned content (`git hash-object --stdin-paths`) IS the staged blob and whose working-tree mode IS the
  staged mode, with a plain `git update-index` (`refresh_index`), so `git status` reads the rewritten tree clean while
  a real edit or an unstaged mode change stays unstaged. Not `git update-index --refresh`: it trusts the stale
  recorded size and answers "needs update" for exactly these files (git's `ie_modified`); `--fix-writers` adds `newline="\n"`
  to a new writer. The PowerShell generators of tracked files write through `scripts/LfText.psm1` (`Set-LfContent`);
  the C# regeneration modes already wrote `\n`. `merge.renormalize` is the documented per-clone setting
  (CONTRIBUTING.md). The entry files below are written LF. A ONE-TIME migration script
  (`scripts/devlog_migrate.py`, deleted by the landing that runs it last, on main's `DEVLOG.md` after its final rebase,
  and which writes the history's range into `DEVLOG/README.md`) split every entry of the 7.1 MB
  mixed-CRLF `DEVLOG.md` into files with LF line endings (PB3029), PROVED by rendering them byte-identical to the
  LF-normalized monolith, and deleted `DEVLOG.md`, `prepend-devlog.py` and `landing_devlog.py`. The history keeps
  every header as written; the twenty numbers the monolith used twice (008-014, 171-185) and 1191a-1191d carry a
  letter in the FILE name (`0185b-…` above `0185a-…`) so the render keeps the monolith's order; CLAUDE.md rule 6, the skills and every brief that said "the TOP of
  DEVLOG.md" changed in that landing (`session-probe` and the ledger read no DEVLOG; the `devlog` memory and the public
  `brent-tools:devlog` skill live outside this repository and are the attended session's to update). `enqueue.py
  land` (PB3023) stamps, one branch per landing: a branch that merged an unlanded predecessor (a same-file successor,
  a review finisher; `prune_worktrees.contained`) lands that predecessor THROUGH it, so `devlog.py stamp` folds every
  pending file the branch carries into its one entry, oldest first, each body under its own title
  (MANDATORY-PRACTICES L8). A predecessor reviewed CLEAN lands on its own instead (PB3381), and a branch is landed
  ONCE (PB3693): the survey holds a WAITING TO LAND successor behind its WAITING TO LAND or ENQUEUED predecessor
  (`prune_worktrees.carrying_landing_branch`), `enqueue.py land` waits (exit 15) while an open pull request already
  adds the stamped entry of a pending title the branch carries (`open_predecessors`' pending arm), and `devlog.py
  stamp` drops a pending entry whose title is already a stamped entry of the tree instead of folding it a second time.
  The stamp is all or nothing (PB3367): its git writes retry on `index.lock`, a failure restores the pending bytes,
  resets the index and prints git's stderr (exit 3, which `land` maps to exit 2, retried once by the lane), and an
  interrupted stamp (entry written, pending gone, uncommitted) is completed by the next run. Rule 6's substance is untouched: one entry per
  landing on main, newest first, the only history.
- **Generated indexes leave the tree (PB3026, landed):** a derived index is a RENDERED VIEW: gitignored, rendered on demand
  by its generator, never committed. `scripts/rendered_views.py` holds the ONE registry (`VIEWS`): `docs/DRIFT_RULES.md`
  (`drift_rules.py --out`; its count line was rewritten by every PR that added a drift test), the `kb/Conformance`
  burn-down (`gen_conformance_notes.py --out`; STALE in the probe for days, now rendered by the probe itself at every
  session start), `docs/DIAGNOSTICS.md` (`gen-diagnostics-doc.ps1`, rendered by `DiagnosticRegistryDriftTests`), the
  `kb/Reference` and `kb/Grammar` vault views, and the ledger page (rendered into the coordination directory). A new
  generated index is one row there plus a `.gitignore` entry. `rendered_views.py --check` is the gate's `RENDERED VIEWS`
  audit (it runs before the slot, so its cost is every gate's: the ledger view's `--check` measures every commit since
  the trend's last recorded point through two git processes, `gen_ledger.measure_commits`, whose self-test holds the
  process count constant in the number of commits: kb/Work PB3315) and a step of CI's `audits` job, and enforces per
  view: not tracked (`git ls-files` ∩ outputs = ∅), gitignored when inside the tree, its generator's own `--check` renders
  it in memory (a drift test with no `<summary>`, an inventory the burn-down cannot read, a plan the ledger cannot read),
  plus a sweep that no tracked `docs/` or `kb/` markdown declares itself generated without a row. Nothing compares a view
  with a copy on disk, so "absent" and "stale" no longer exist; a reader that needs a view (`phase_b_batch.py` reads the
  diagnostics table) refuses with the render command when it is absent, never reads an empty one as "no diagnostics".
  No test asks the script (a Unit test did until kb/Work PB2878: its tree sweep reads the head of every tracked `kb/` note,
  and a test that read the vault made every note a test input), so the audit and CI are its only callers. NOT views, and
  tracked on purpose: generated SOURCE the build compiles (`*.g.cs`, `*.Table.cs`: a clone must build with no pre-step; each
  has its own drift test), DATA extracted from the licensed standard (the generator needs the private PDF), the gating
  status block embedded in `VERSION_CHANGE_REFERENCE.md` (a region of a hand-written, anchor-checked document: one row per
  construct, sorted, so it merges line by line; separating it is a lead, not part of this change) and `kb/Work.base` (a view
  DEFINITION, a query over the notes).
- **Plan §0 and other shared prose** are edited only by the loop's own `ledger`/§0 refresh PR, never inside a fix PR; a
  fix PR edits its notes, its code, its tests, its goldens and the design doc of its subsystem. `CONFORMANCE.md` was measured
  (93 commits in the week to 2026-10-25; kb/Work PB3643 counted 126 conflict hunks over it) and becomes one source file
  per determination with a rendered index: the design is `DESIGN-orchestrator-loop.md` §16.8 (kb/Work PB3726, PB3738).

#### 3.17.5 The loop's units and the fleet after this design (PB3023, PB3024, PB3027; DESIGN-orchestrator-loop.md §3, §4.7, §9)

- `land` became **`enqueue`** (PB3023, `units/enqueue.md`), and its landing step moved into the supervisor's
  **landing lane** (PB3103, `land_lane.py`, DESIGN-orchestrator-loop.md §4.9): beside whatever unit runs, the lane
  runs `enqueue.py land` for every WAITING TO LAND branch (`prune_worktrees.py`'s survey, in harm order) the moment
  it is ready and retries what a script can see end; the unit reads `land_lane.py status` and dispatches ONE
  `cobol-finisher` (`finisher-brief.md`) for a lane attempt stopped on a conflict, a red gate, a DEVLOG or oracle
  refusal, or a queue bounce (`prune_worktrees.py` BOUNCED, from one
  GraphQL read of the open pull requests: in the merge queue → ENQUEUED, removed from it at the branch's tip → BOUNCED
  with GitHub's reason; offline the states are git-only); one `cobol-clerk` files the reports' leads; render the ledger;
  hand off. It is mostly a script run; its model turns are the review and finisher dispatches and the handoff. While
  the queue is not ACTIVE, `enqueue.py land` refuses before it merges or gates anything (exit 8): main takes only
  queued pull requests (the ruleset binds administrators), so a direct-push arm could land nothing and gated half an hour
  with no lease held (review R1-1, corrected at implementation; the phase-1 bridge that kept the direct-push landing
  until the owner's ruleset go retired once the ruleset was ACTIVE).
- The **train, the lander role, `lander-train-brief.md`, `lander-brief.md` and the train applier are retired** (PB3023;
  CLAUDE.md rule 4: deleted, never wrapped); `golden-lander-brief.md` became `golden-lane-brief.md` (the lane-1
  integrator, landing through `enqueue.py land --lane golden`); `prune_worktrees.py --train` and `TRAIN_MAX` are gone (the landing
  order is `--brief`'s); `dispatch_guard.py`'s lander rule went with the role. `cobol-lander` became `cobol-finisher`
  (resolve a conflict, fix a red or a bounce, gate and return DONE: its commits are reviewed on the branch before the
  landing lane lands the new tip; turn cap 160). The landing lease, the direct-push landing script and its `ci/**`
  branches are deleted (PB3024's closing change, the first branch cut after PB3023 landed).
- `wf_rolling_wave.js` **starts no lander** (PB3023): a wave produces DONE branches reviewed CLEAN; the landing lane
  lands each as it comes back, while the wave runs (PB3103). This removed PB3016's policy question (may THIS wave land?) with the mechanism; `train_size`,
  `min_final_train`, `lead_ids_per_train` and the lead-id blocks are gone, and the planner prices each group's landing
  at `landing_tokens` (`model_rules.json`) instead of one lander per train.
- **Measurement** (PB3028, landed): `landing_measure.py` replaced `train_measure.py`, which is deleted with its drift
  test. One record per pull request in `<coord>/landing-measurements.jsonl`, built up by patches because no tool sees
  a whole landing: `enqueue.py queue` writes the enqueue time, each carried change's DONE (`done_times`: the committer time of the HEAD commit its DONE implementer report names, never the report file's time, which the branch review's stamp rewrites; the changes are the reports naming the branch plus those a train commit's `Report:` line names, one per lead note, each the earliest of its finisher chain; a report whose HEAD is already an ancestor of origin/main is a change that landed before this one and counts for nothing, because the queue's MERGE method makes every landed head an ancestor of every later branch) the dispatch and review-clean times (`stage_times`, kb/Work PB3638: `dispatched` the dispatch ledger's time of the earliest dispatch still recorded for a lead note the branch's reports answer, `review_clean` the time of the CLEAN verdict record the newest report's stamp names; a time out of order stays null; the order is judged against the record the enqueue will patch, so a re-enqueue after a bounce and a re-review keeps the first `enqueued`, the earliest `dispatched` and `done`, and leaves out a CLEAN verdict, and a DONE (`done_times` is bounded by the first enqueue, not this call's time), that follows the first enqueue, kb/Work PB3710) and the worktree's local gate seconds (the sum of its `verdict.json` `timings.wall_s`);
  `record --review-clean` (a hand correction) and `--bounce-causes` (cluster-defect · load-timing · conflict · infrastructure) are named by
  whoever knows; `complete` reads `mergedAt`, the count of FAILED merge-group runs of the PR (a run GitHub cancelled
  because an earlier entry failed is a ripple, not a bounce), the count of FAILED `pull_request` runs on the PR's head
  branch (`pre_queue_reds`, kb/Work PB3096: a PR red before the queue never reaches a merge group, so `bounces` cannot
  see it; a branch with no pull-request run at all stays null) and the successful run's wall from `gh`. A field never
  observed stays null and prints UNMEASURED (a missing observation is not a zero); a bounce with no named cause prints
  UNATTRIBUTED. `summary` prints changes landed per hour, median and p90 minutes DONE → on main per change, bounces per landing and
  their causes, pull-request reds before the queue per landing, merge-group CI wall and local gate minutes, and the stages of the owner's fix-cycle bar (kb/Work PB3638: dispatch → DONE → review clean → enqueued → merged, median and p90 each, and dispatch → enqueued against 30 minutes and enqueued → merged against 15, a median over its budget printed OVER, a defect against PB3638) as the AFTER column beside the BEFORE column from the retired
  train records (batched gating) and the frozen 2026-10-10 evidence; `freeze` copies the train records and the first
  after-summary under `evidence/fleet-optimization/` once a PR has landed. The completion ledger's landing rate and its
  minutes per landing come from the same file (`landing_measure.landing_rate_input`: the queue's own from two landings,
  else the trains', whose minutes are lander start → push, a floor; the headline names what its minutes measure and the ledger prints that label). The design is judged by changes landed per hour and minutes per landing, before and after.

#### 3.17.6 CI and repository settings (PB3024)

- `build-and-test.yml` (landed, PB3024 phase 1): `merge_group:` added; the `changes` job runs the matrix on
  `merge_group` (base = the group's `base_sha`, so a group of inert-lane PRs skips it exactly as an inert landing did, §3.18)
  and `workflow_dispatch`, only the cheap jobs (`audits`, `ci-gate`) on `pull_request`, and on `push` to main the
  already-verified short-circuit (the merge-queue commit IS the commit main advances to; measured on the first three
  queued landings, PRs 7–9: each `push` run found its merge group's green run on the same sha and skipped the matrix).
  ⛔ The short-circuit counts only a successful run of the workflow from an event other than `pull_request`: a PR's
  cheap `ci-gate` is attached to the PR's HEAD commit, so a check-run lookup would have read it as a verdict on the
  matrix. `push.branches` is `[ main ]` (PB3024's closing change dropped the `ci/**` landing branches), and the
  DEVLOG-entry check runs only on `pull_request` and `merge_group`. `ci-gate`'s `needs:` and its drift check (every job
  wired) are unchanged. The `supersede` job (kb/Work PB3769) runs on `merge_group` only and is the one job granted
  `actions: write` (with `contents: read` for its sparse checkout and `pull-requests: read` for the queue's entries); it
  cancels the runs the queue has replaced (§3.17.2 step 4).
- A **ruleset** on `main` (classic branch protection has no merge queue, and GitHub offers a merge queue only to an
  ORGANIZATION-owned repository — the second apply, 2026-10-11 18:44, was refused with `Invalid rule 'merge_queue'` on
  the personal account's repository, so the repository is `WiseOwlSoftware/COBOL`: owner decision 2026-10-11, kb/Work
  PB3078): require a pull request, require the merge
  queue with merge method **merge commit** — the branch's commits stay ancestors of `main`, so the branch lifecycle
  (`prune_worktrees.py`'s landed test, `carried()`, `git cherry`) keeps working offline, and `git log --first-parent
  main` reads one merge commit per landing — GitHub's `Merge pull request #N from …` subject with the PR's one-line
  title as its body. GitHub pairs the MERGE_MESSAGE title only with PR_TITLE (its three pairs are PR_TITLE+PR_BODY,
  PR_TITLE+BLANK and MERGE_MESSAGE+PR_TITLE; the first apply, 2026-10-11 16:27, asked for MERGE_MESSAGE+PR_BODY and
  was refused), and PR_TITLE as the subject would put `Train <label>:` on the merge commit, which
  `prune_worktrees.py`'s `train_heads` reads as a train's last commit. The forensic record of a landing is therefore
  the branch's own commits beneath the merge commit and the PR body, never the merge commit's message;
  `ruleset.py` refuses any pair outside GitHub's three before it calls GitHub. Squash was considered and rejected on the operator's review (2026-10-11 12:16): a
  squashed head is never an ancestor of main, so every lifecycle tool would have to key on the merged-PR record instead.
  Required status check `ci-gate` on the merge group, enforce for administrators (the owner's credentials make every
  push), minimum group size 1, maximum 5, check timeout 60 min. The classic protection is removed in the same change.
- **Cutover (operator review item 4; owner fast track ~12:58, "Why are we still using push-main?" → "Fast track now"):**
  every step of this design lands through the THEN-CURRENT pipeline. **Phase 1 = PB3024, landed FIRST and ALONE (a
  blocking one-cluster landing), as the last landing through the direct-push landing script:** the `merge_group` trigger and the
  cheap-PR arm, the committed ruleset (`scripts/orchestrator/ruleset-main.json`) and `ruleset.py check|apply`, and the
  pull-request step as a SCRIPT, `scripts/orchestrator/enqueue.py queue` (the landing checks, push the branch, open or
  reuse its pull request, queue it with auto-merge; no CI wait), with `enqueue.py checks` the one landing-check
  sequence both paths run. **⛔ The operator's review (2026-10-11 13:39, mailbox 20261011-133923):** with
  `pull_request` limited to the cheap jobs, a PR opened BEFORE the owner applies the ruleset has no merge-group run,
  its `ci-gate` passes on the cheap jobs, and auto-merge would land it with NO whole-population run. Closed by
  construction: `enqueue.py queue` refuses (exit 8) before any check, push or pull request unless `ruleset.py check`
  reports the queue ACTIVE (live ruleset equal to the committed one, the queue present, the classic protection gone);
  no other tracked file opens or queues a pull request (`LandingPipelineDriftTests`, §3.17.8); until the queue was
  ACTIVE the lander kept landing through the direct-push script. That script, the landing lease and their drift tests,
  the `ci/**` trigger and MANDATORY-PRACTICES L3/L13's references to them were deleted by PB3024's closing change, cut
  right after PB3023 landed — not by the first queued landings (PB3077, PB3078), because PB3023 rewrote 39 of the 48
  files that retirement touches (kb/Work PB3078). **The switch happened 2026-10-11 19:14:** on the owner's go (16:27) the attended session ran
  `python scripts/orchestrator/ruleset.py apply --owner-go "<the go>"` (it also set the repository's auto-merge and
  delete-branch settings) — after the repository's transfer to the WiseOwlSoftware organization, the merge queue being
  organization-only (PB3078); from that instant main accepts only queued pull requests.
  **Phase 2 = PB3023 (`enqueue.py` gains the rebase, the one local gate and the DEVLOG stamp) and PB3025 (the
  per-entry DEVLOG and its CI check)**, each through the queue.
- `guard` (12.8 min) is the long pole of a merge-group run; sharding it is a follow-up measured after the queue lands.

#### 3.17.7 What each document says after this lands (the implementer's sweep; rule 6, same change set)

`CLAUDE.md` (rule 6's DEVLOG shape; the Testing paragraph: one gate mode, CI as the regression net, the merge queue,
no WSL gate, the CI invariant restated as "a PR bounced by the queue is a defect fixed on its branch before re-enqueue")
· `MANDATORY-PRACTICES.md` L1–L3, L10–L13, O10, O11 · `.claude/skills/{gate,land-a-fix,workstream,session-start,
kb-sync}` · `scripts/orchestrator/units/{land,wave,resume}.md` → `enqueue.md` · `DESIGN-orchestrator-loop.md` §3, §4.7,
§9 · plan §0 "Gates" and §9 · `docs/DOC_INDEX.md` · `.claude/agents/cobol-lander.md` → `cobol-finisher.md` ·
`scripts/session-probe.ps1` (the `gate :` line) · `scripts/hooks/dispatch_guard.py` (refuses a lander dispatch: there is
none) · §3.14.1 (one mode), §3.15 (the WSL gate becomes the Linux-red REPRO tool, `linux-gate.sh` kept for that use
only, out of every gate path and brief).

#### 3.17.8 Drift tests that pin the design

`LandingPipelineDriftTests` (phase 1, landed): the workflow has a `merge_group` trigger, a `pull_request` arm that
skips the matrix, and a short-circuit that never counts a pull request's run; no tracked file outside the narrative paths (docs, kb, history, tests) opens or
queues a pull request except `enqueue.py`, and its queue guard precedes its checks, push and pull-request calls; the
self-test arms of `enqueue.py` and `ruleset.py` are declared. The live ruleset is compared by `ruleset.py check`, whose
consumers are the landing (exit 8 on anything but ACTIVE) and the session probe's `gate :` line, not an xUnit arm (the
suite has no dynamic skip, and an offline comparison must read SKIPPED, never green). Landed with PB3024's closing
change (`TheDirectPushLanding_IsRetired_AndNothingNamesIt`, with its planted-name arm): the direct-push landing
script (both forms), the landing lease, the train applier and the landing oracle (kb/Work PB3107) do not exist, the
push trigger is `[ main ]` with no
`ci/**`, and no tracked file outside the history paths (`DEVLOG/`, `docs/rearchitecture/evidence/`, `kb/Work/`,
`adjudication/`) names any of them by file name. A later arm, with the change that retires it: no generated index is tracked
(`git ls-files` ∩ the generators' outputs is empty). Landed with kb/Work PB3022's first part: `GateLegDriftTests` (no
caller names a gate mode) and `HostWitnessTraitDriftTests` (`TIER_ORDER` contains `0h` and every race-witness class
carries the `Host` trait). Landed with kb/Work PB3025: `DEVLOG.md` does not exist and every `DEVLOG/*.md` has a
well-formed header and a unique number (`devlog.py --self-test`'s `lint` arm over the repository's own `DEVLOG/`, run
by every gate's self-tests and CI's `audits` job). Landed with kb/Work PB3107: `NoTrackedRecord_DescribesOneBranchsTree`
(no committed arch-oracle baseline, and `capture_oracle.py` has no `--record`), the rule of §3.17.2 step 4 that no
landing is held to a record of its own tree. Landed with kb/Work PB3769:
`TheMergeGroupRun_CancelsTheRunsTheQueueSuperseded_AndNothingElse` (the `supersede` job runs on `merge_group` alone
with no `needs`, runs `cancel_superseded.py` with the run's id and head commit, is the only job with `actions: write`,
has every step `continue-on-error` (kb/Work PB3820: the checkout too, the script gated on the checkout's outcome, a failed checkout said as a warning; `StepsThatCanFailTheJob` is the step reader, held by its own arms) and is a `needs` of `ci-gate`; the arms of `cancel_superseded.py` and of `queue_facts.live_heads`
are declared), the supersession rule of §3.17.2 step 4. Landed with kb/Work PB3787: `SparseCheckoutDriftTests` (reader:
`SparseCheckoutAudit`): every workflow job whose checkout has a `sparse-checkout` list (`supersede`, `dotnet-sdk-cache`)
lists each repository file its steps name, each file its local action names, and each repository module a Python script
it runs imports as it starts, transitively (a function body's import is lazy and not followed; `importlib` and path
strings are the static read's limits), and sets `sparse-checkout-cone-mode: false`, the mode the list is read in; the
arms hold the real workflow (without `scripts/standard_streams.py` it is flagged), a transitive import, a lazy import,
a docstring, relative imports, packages, a local action, comments, the pattern forms, a cone-mode job and a negation.

### 3.18 THE INERT LANE — a change no test reads runs the audits and nothing else (kb/Work PB2878)

**Why.** Owner 2026-10-09, on a one-change landing that edited a practices file and two briefs: "Should this require a
full CI run? I would think not." Of the 300 landings of 2026-10-16..23, 137 changed only paths CI's doc-only regex
named, and each still paid the local gate (median about 14 min: the audits, a slot, a build, leg 1) and the landing's
four Linux legs. And the regex was not a classification: `docs/` and `kb/Work/` are test INPUTS, so CI skipped the
very tests that read them, and a kb/Work-only branch went RED on the landing's Linux gate behind a GREEN Windows gate
(kb/Work PB3603); the CI invariant (PB1957) held only because the Linux gate ran every leg.

**The classification — ONE module, `scripts/inert_lane.py`.** A path is INERT when it lies under an `INERT` entry and
under no `READ` entry; each entry names who reads it (an audit, a person; a READ entry names the tests that read it).
A change is in the inert lane when its changed paths (`git diff --name-only --no-renames`, so a code file renamed into
an inert directory shows its old name too) are non-empty and all inert. The INERT entries are `DEVLOG/`, `kb/` (the
knowledge base, THE work register `kb/Work/` among it) and `.obsidian/`. NOT inert, measured on 2026-10-23 by deleting
each candidate and running the three assemblies on Windows, and then by the watch below over the whole Linux
population: `docs/` (SortRuntimeDocShapeDriftTests and GateLegDriftTests read every doc, and twenty-odd classes read
CONFORMANCE.md, the rule catalog or VERSION_CHANGE_REFERENCE.md); `.claude/` and the root `*.md` files
(GateLegDriftTests, ArchReviewFleetDriftTests, SelfTestDiscoveryDriftTests); the `tools/claude-skills` pin, which the
self-tests initialize. Two sweeps moved to make `kb/` inert, the way the conflict-marker sweep moved before them:

- **The conflict-marker sweep**, which read EVERY tracked file and so made every path a test input, became an audit
  (`scripts/conflict_markers.py --check`, kb/Work PB735's rule unchanged), which also closes the hole that it never ran
  in CI on a change whose matrix was skipped.
- **The register's cross-checks against the inventory** (every defective-verdict row claimed by a live note; every
  landed `kind: defect` note naming the rows it closed, or why none, and every row so named closed in the inventory)
  were two Unit drift tests that read every `kb/Work/` note through a C# twin of `work.py`'s frontmatter reader. They are
  now `scripts/spec/work_inventory.py`, called by `work.py check`, so the register has ONE reader and the checks are
  audits: the gate's `WORK REGISTER` audit runs them before the slot, CI's `audits` job on every event, and they run on a
  `kb/`-only change exactly as on any other. The twin, its parity fixture's second engine and `work.py parity` are gone
  (the fixture stays as `work.py --self-test`'s cases). The tree sweeps of LandingPipelineDriftTests (two scopes),
  LandingLaneDriftTests and LandingMeasureDriftTests set `kb/` and `.obsidian/` aside whole, and EcRaiseSiteRegistryDriftTests
  no longer reads a note (the raise site is its evidence). `RenderedViewsDriftTests`, which only ran
  `rendered_views.py --tree` (rule (c) there reads the head of every tracked `kb/` note), is deleted: the same rule is
  `rendered_views.py --check`, the gate's `RENDERED VIEWS` audit and a step of CI's `audits` job, so no test reads the
  vault.

**Who calls it — every runner, no list of its own (the CI invariant).**

| Runner | Inert lane | Full lane |
|---|---|---|
| CI `changes` (`classify --base <group base or before> --head <sha>`) | `run_matrix=false`: `audits` + `ci-gate` | the matrix |
| local gate (`run_gate_legs.py`, `Host.lane`, over the measured working tree against the base) | the audits, then (on a host that runs the Linux half) the Linux script self-tests in a WSL clone of HEAD under a slot (`linux-gate.sh --legs inert`), then `… GREEN — INERT LANE (kb/Work PB2878): …`, no build or test leg; under either scope | the ordered gate |
| Linux gate (`linux-gate.sh`, HEAD against its merge base with origin/main; the targeted and inert modes take the lane from the gate that started it) | no test leg and no build, but the script self-tests (`selftests`, no clone build): `=== LINUX GATE: GREEN (inert lane, …; legs selftests …) ===` | every leg, under the watch |

The audits run in every lane (they are where a register note, a citation or a DEVLOG entry is checked); CI's `audits`
job runs on every event.

**The script self-tests run on Linux in the inert lane too (kb/Work PB3646).** CI's `audits` job runs `self_tests.py` on
Linux on every event, and those self-tests are audits that read the inert paths by design (the register, the DEVLOG,
the skills submodule they initialize), so the inert watch cannot vouch for them and a `kb/` or `DEVLOG/` change can turn
them red on Linux. Until PB3646 both local Linux processes ended GREEN in the inert lane before any of them ran, so
CI could fail on code the local gates passed (the CI invariant, PB1957). Now `linux-gate.sh` runs its `selftests` leg
in the inert lane (a clone of HEAD, no build, no corpus: measured 25 s of self-tests, 45 s for the whole run) and
`--legs inert` is the same run for the Windows gate, whose inert lane takes a LIGHT slot (§3.14.6, kb/Work PB3724: its own pool and cap, so a notes-only landing never queues behind the full gates' builds), runs it after the audits
(`Gate._inert`, `Host.linux_leg(inert=True)`), records it as the Linux half, and ends BUSY, NOT RUN or RED on it exactly
as the full lane does. `tested_locally` and the gate's reuse rule therefore require a GREEN Linux half for an inert
record on a host that runs one (an inert record from before PB3646 has none, and the gate runs again).
`InertLaneDriftTests` holds the wiring. A Linux host's audits ARE the Linux self-tests.

**The proof — the inert watch.** A list of "what no test reads" that nobody proves drifts from the tests, and a test
that starts reading an inert path would let an inert change land untested. So every whole-assembly test run on Linux
runs under `inert_lane.py watch`: an inotify watch (through `ctypes`, no package to install) of every directory an
inert tracked file lies in and of every inert directory itself, so an OPEN of an inert file or a LISTING of an inert
directory, by the test or by any process it starts, is recorded; the run is RED naming each such path (`opened: …`), and
an overflowed event queue is RED too (it proves nothing). CI's four Linux `dotnet test` steps run under it; the Linux
gate runs one watch over its parallel test legs (the `inert-watch` leg) in a clone that runs those three legs and nothing
else, and runs the script self-tests (audits, which read inert paths by design: the register, every DEVLOG entry, the
skills submodule they initialize) and the guard in a second clone with its own build, the AUDIT clone (which also takes
the guard's own `dotnet build` of the CLI off the clone whose CLI the legs run, kb/Work PB3529). Windows has no open
events, so the
Windows runs carry no watch; the Linux runs are the whole population on every code change. The answer to a RED watch is
a READ entry naming the reader (the path leaves the inert lane) or making the reader an audit, never deleting the watch.

**Drift test.** `InertLaneDriftTests`: the `changes` job classifies through the module and carries no path regex; every
`dotnet test` line of a Linux job runs under the watch; `linux-gate.sh` classifies and watches, and `run_gate_legs.py`
classifies, through the module; `inert_lane.py check` is GREEN (every entry names a tracked path, every READ entry lies
under an INERT one). The classification's logic and the watch's firing (a read, a listing, a READ path, the command's
own exit code) are `inert_lane.py --self-test`'s arms, run by the self-test runner in every gate and CI (the inotify
arms on Linux: CI's `audits` job and the Linux gate's `selftests` leg). `SelfTestDiscoveryDriftTests` holds both
self-test legs of the Linux gate to the audit clone.

**What the lane covers.** A notes landing changes `DEVLOG/` and `kb/Work/` (107 of the 137 landings above changed only
those), so it is the inert lane: the audits (the register's checks among them) and nothing else. A change to `docs/`, a
skill or a brief is still the full lane until its readers become audits or gain READ entries, one measured path at a
time (an INERT entry is added only with a GREEN Linux watch run: `bash scripts/linux-gate.sh --nice`, its `inert-watch`
leg).

---

## 4. Current → target module changes

| Action | From | To | Why |
|---|---|---|---|
| create | — | `tests/WiseOwl.Cobol.Tests.Characterization/` (+ `tests/characterization/`, `Snapshots/`) | The behavior-neutrality harness (gates 2 & 3) — the missing "prove I changed nothing" proof for every refactor phase. |
| create | — | `tests/nist/corpus.tsv` | The source of the green NIST set + chains, and of the guard's population: it replaced all three copies, the last the serial guard's `NIST_TESTS` (kb/Work PB925, PB3383). |
| create | — | `src/WiseOwl.Cobol.Compiler/Diagnostics/` (`DiagnosticDescriptor`, `Diag`, `Diagnostic`, `Severity`) | Registry so codes are addressable, snapshot-keyable, doc-generable, suppress-targetable. |
| (exists) | — | `docs/REARCHITECTURE_PLAN.md` | Resumable migration SSOT + roadmap with per-phase exit criteria = battery-green gate (the master plan already in the repo). |
| create | — | `docs/DIAGNOSTICS.md` (generated) | The single human-readable code table (understandability #1). |
| create | — | `tools/DifferentialBakeTool` (or a skip-gated maintenance test) | One-time freeze of legacy output → committed goldens (the G8-survival bake). |
| create | — | `scripts/guard.ps1` | Cross-platform authoritative guard (Windows parity for the regression). |
| split | `NistDifferentialTests.cs` (318 `[InlineData]`) | `[MemberData]` over `corpus.tsv` + a thin theory | Remove the hand-maintained green list; single source of truth. |
| refactor | ~60 `*DifferentialTests.cs` (assert `wiseowl-cobol==legacy`) | golden-comparison base (`AssertMatchesGolden`) + `tests/differential/**/*.out` | Sever the oracle coupling so the net survives G8. |
| refactor | `EditionContext.Error(code,msg)` string-concat; 161 bare codes | `sink.Report(Diag.X, args)` | Route all diagnostics through the registry. |
| split | `COBOL0899` (~47 sites) | distinct unimplemented-feature descriptors behind a tracked list | One code = one rule; snapshot precision. |
| move | `CompilerUnderTest.LegacyCompiler` / `ICompilerUnderTest` | DELETE after the bake | Legacy oracle gone once goldens are baked. |
| refactor | `RoslynBackend.ReferenceAssemblies()` (uncached) | `static Lazy<ImmutableArray<MetadataReference>>` | Battery throughput (thousands of rebuilds → one). |
| refactor | binder passes behind the `CSharpEmitter.Bind` host facade | `Binding.BindPipeline → BoundCompilation` (standalone) | Bind phase boundary: fast `CheckOnly`, probe-able bound tree, clean Emit snapshot. |
| rewrite | `.github/workflows/build-and-test.yml` (4 jobs, legacy-authoritative) | OS-matrix `build-test` + `version-sweep` (the temporary `legacy-oracle` job was deleted, PB2108) | Greenfield-authoritative, cross-platform NIST, characterization gated. |
| deleted (PB2110) | the legacy unit and integration test projects | — | Retired with the engine. |
| deleted (PB2109) | `scripts/compliance.sh`, `nist-batch.sh`, `run-suite.sh` and the guard scripts' legacy arms | — | The guard scripts themselves stay: they are the CLI-level NIST leg over `cobol` (PB750). |
| delete | `CobolParserJsonXml.g4`, `CobolExtensionsJsonXml.g4` (dead JSON/XML) | — | Non-ISO (0 spec occurrences) — hard invariant #5; remove from any test/build surface. |
| move | legacy `ConformanceTests` corpus + `GreenfieldOnly`/`LegacyDivergent` sets | folded into `CorpusRunnerTests` | One corpus runner over committed goldens. |

---

## 5. Migration notes — keeping the battery green through the phases

The rearchitecture proceeded so the net was STRENGTHENED before it was relied on, then legacy was severed:

- **Phase R0 — Net-first (do BEFORE any refactor).**
  1. Land `corpus.tsv` + `CorpusManifestTests`; repoint `NistDifferentialTests` at it (behavior identical, coverage
     unchanged — pure de-duplication). 2. Stand up `WiseOwl.Cobol.Tests.Characterization` and SEED `Snapshots/` from the
     CURRENT emitter (captures "today's behavior"). 3. Cache the Roslyn reference set (safe, pure speed). 4. Land the
     diagnostic registry + `DiagnosticSnapshotTests` seeded from today's output. After R0 the battery can prove
     behavior-neutrality; no compiler behavior changed.
- **Phase R1 — Bake the oracle (DONE).** The legacy output was committed as `tests/differential/**/*.out` and the differential
  base rewritten to golden comparison, so the `wiseowl-cobol==legacy` asserts are gone and the net is self-standing.
- **Phases R2…Rn — the actual rearchitecture** (god-class splits, pass pipeline, storage-form unification, per the
  sibling DESIGN-* docs). EACH phase: run the full battery; gates (1)+(2) MUST stay green; gate (3) stays green or
  is reviewed-re-baselined IN THE SAME change set (with a DEVLOG note citing the intended emit change and the
  gate-(1) proof). No phase merges red. Small phases (feedback_tiered_gates) so a snapshot diff is legible.
- **Phase G8 — Sever legacy (DONE, PB2108–PB2110).** The legacy test projects, the legacy `ProjectReference`s, the legacy guard
  arms, the `legacy-oracle` CI job and the engine itself are deleted. The `build-test` matrix is the whole gate, cross-platform.

Throughout: `Generated/` remains a build output (regen per checkout, failed regen fails the build); warnings-as-
errors stays on the Release/CI build; the drift tests (`ConstructRegistry`, `ReservedWords`, `CorpusManifest`,
`DiagnosticRegistry`) are the "nothing silently added/dropped" backstop.

---

## 6. Risks

1. **Snapshot brittleness.** Emitted-C# snapshots (gate 3) will diff on almost every emitter refactor. MITIGATION:
   gate (3) is advisory/reviewed, never a hard CI red on its own IF gates (1)+(2) are green — but a gate-(3) diff
   with NO corresponding source change in the PR IS a red (unexpected drift). Keep the characterization corpus small
   and representative (one program per feature family), not the whole NIST set.
2. **Bake faithfulness.** If the bake captures a legacy output that WiseOwl COBOL already diverges from (an
   intended ISO fix), the golden would wrongly pin the legacy value. MITIGATION: bake only cases where the
   differential test is currently GREEN (wiseowl-cobol already == legacy); a currently-red/skip differential case is
   hand-authored to the ISO value and marked `divergent`.
3. **Two runtimes side-by-side until G8.** The conformance project references both runtimes; a program loads only
   its own. Low risk (proven today) but the characterization/probe path must use the greenfield driver ONLY.
4. **`corpus.tsv` chain semantics.** The manifest-driven run was verdict-equivalent to the old sources (349/349,
   DEVLOG entry 667), and `chains.tsv`, the `[InlineData]` list and the serial guard's hand-ordered `NIST_TESTS`
   are deleted (kb/Work PB925, PB3383).
5. **Diagnostic registry churn** touches ~161 call sites. MITIGATION: mechanical, one code family at a time, each
   change set battery-green; the drift test catches any orphaned/duplicate code.
6. **CI time** could rise if characterization emits Roslyn-compile every program. MITIGATION: gate (3) snapshots the
   emitter STRING only (no Roslyn); the reference-set cache offsets the rest.

---

## 7. Open questions for the owner

1. **Snapshot tooling:** hand-rolled `Assert.Equal(File)` + `WISEOWL_COBOL_UPDATE_SNAPSHOTS` env, or adopt Verify
   (`Verify.Xunit`)? Verify gives received/verified diffing + review workflow for free but adds a dependency. Lean:
   hand-rolled (zero new deps, matches the repo's existing golden-file idiom) unless you want the Verify UX.
2. **Characterization corpus size:** one program per feature family (~120 programs, fast) vs. snapshot the full NIST
   corpus's emitted C# (maximal coverage, slower, noisier diffs). Recommend the curated family set for gate (3), NIST
   goldens already cover gate (1) breadth.
3. **When to sever legacy (G8 timing) — RESOLVED (R69, 2026-10-06).** The legacy engine was deleted without a live cross-check
   job and without an equivalence proof, because the one guard has driven `cobol` since PB750; the archive is the tag in
   `docs/rearchitecture/LEGACY-ARCHIVE.md`.
4. **`--suppress` granularity:** per-code, per-family, or per-descriptor `SuppressKey`? Affects the registry shape;
   default proposed is per-code with an optional family key.
5. **Migration-SSOT vs resume-prompt.md ownership — RESOLVED (2026-07-07):** the migration roadmap is the standalone
   `docs/REARCHITECTURE_PLAN.md` (since 2026-07-19 THE ONE consolidated plan incl. the §0 live banner);
   the feature-drive state ALSO lives there now — `resume-prompt.md` was absorbed into it and DELETED (2026-07-19).
6. **Do we snapshot the runtime deploy?** The runtime DLL is copied per emit; characterization ignores it. Confirm
   the runtime is out of the neutrality scope (it is typed-native and separately unit-tested) — assumed yes.
