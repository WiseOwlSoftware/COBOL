# MANDATORY PRACTICES — every dispatched agent, every time

⛔ **Owner standing instruction 2026-09-23: "All these best practices must be durably recorded for all future to
must use."** This file is the ONE place they live. Every brief in this directory points here, every dispatch spec is
rendered by `make_dispatch_specs.py` (never hand-written in a scratchpad), and `check_practices.py` fails when a brief
or a rendered spec drops one. A practice that is only in a transcript or a scratchpad file is FORGOTTEN — the wave-58
dispatch still depended on `GATE-WAIT.txt` and a tail file living in one session's scratchpad.

Each rule carries its reason and its measurement; do not drop a rule because its reason looks stale — re-measure.

## All roles

| # | Practice | Why (measured) |
|---|---|---|
| P1 | **Opus 5.5 via the `opus` alias** (`CLAUDE_CODE_SUBAGENT_MODEL=opus`, `model: 'opus'` on every agent). Never a dated id. | Owner 2026-09-22 (DEVLOG 1635). |
| P2 | **Never end your turn while a background job runs.** Start it to a log, then BLOCK: `timeout 580 bash -c 'tail -n +1 -f <log> \| grep -m1 "<verdict>"'`, re-issued until the verdict prints. push-main: append `echo PUSH-MAIN-EXIT=$?` and block on it. | Wave 45: five implementers + a lander returned "gate PENDING", every gate killed. |
| P3 | **Graceful STOP.** Before each new step check `{SCRATCH}\STOP`; if present: checkpoint-commit, STATUS.md NEXT, report, return SPLIT / landed=false. Never start a build or gate once STOP exists (a lander that has STARTED push-main finishes it). | Owner 2026-09-22: no job may be killed by a quota limit. |
| P4 | **Checkpoint to disk**: WIP commit after every mechanism/step + `STATUS.md` (DONE / NEXT / BLOCKED / GATE / batch paths / codes). Never `git stash` (shared across worktrees). | Session kills cost at most one step. |
| P5 | **Turn caps**: read-only 160, implementer 220, one landing per lander transcript. At the cap: checkpoint and SPLIT, never extend. | tokens ≈ 0.115·T + 0.00031·T² (n = 239). |
| P6 | **Start from the code sites** — the note's file:line, or `python scripts/spec/where.py <clause> [rule]` (every rule is cited in code, so the citations ARE the index). Do not re-survey a subsystem. | Waves 45–57: grep/find/sed/Read = **46 %** of all implementer tokens — the largest single cost. |
| P7 | **Every lead you report carries its repro path and its code site (file:line)**, so the registrar and the next implementer do not re-discover them. | The same fact was being found three times: implementer → registrar re-probe → implementer re-probe. |
| P8 | **Citations through `cite.py --check`**; ids and diagnostic codes only from the orchestrator's allocation; reports at `{SCRATCH}\reports\<wave-slug>-<lead>-report.md` (wave-prefixed). | Inherited-citation defects (CA10); five id collisions in one day; train 48 found reports overwritten. |
| P9 | **`python scripts/semgrep/verify.py` may not increase any count.** | Train 57 dropped PB999 for +20 BigInteger hits nobody reported. |

## Implementer / finisher

| # | Practice | Why |
|---|---|---|
| I1 | Gate = own tests + `~Drift\|~EditionGate` + Unit, **always `-Priority BelowNormal`**; a shared seam (.g4, MOVE, reference resolver, EC emitter) adds `~CorpusRunner\|~Nist`. | Lander's whole-assembly leg went 9.6 → 30.6 min when implementers competed at Normal. |
| I2 | **Never run the whole Conformance assembly.** | Seventeen implementers running it tripled the lander's gate time. |
| I3 | Re-probe every note on your own build first; a non-reproducing note is DISCHARGED with evidence. | Wave 42: three stale notes. |
| I4 | One positive golden at the introducing edition + one negative below it; parser + emitter + golden + manifest in one commit. | Owner 2026-09-13 lever 3. |
| I5 | Flip `status` + write `closes_rows` in the landing commit; `work.py check`; no lists anywhere. | CLAUDE.md rule 8. |
| I6 | Report ≤ 60 lines per `implementer-report-template.md`. | Owner 2026-09-13 lever 4. |

## Lander (train)

| # | Practice | Why |
|---|---|---|
| L1 | 4–6 clusters per landing (target 5); one commit per cluster; one DEVLOG entry; one push. | 10.4 M/cluster at k = 1 vs 5.1 M at k = 5. |
| L2 | Gate = the **WHOLE Conformance assembly, unfiltered** + Unit + Characterization, at **Normal** priority, in the lander's worktree. | Trains 39–41 went red on CI's `rest` shard behind green union-of-filters gates. |
| L3 | **Pipelined**: dispatched while the previous train is still in CI; merge + gate on current origin/main; before push-main BLOCK until the previous head is an ancestor of origin/main, rebase, and re-gate ONLY if the rebase had conflicts outside docs/kb/DEVLOG. | Train 50 waited 44 min for train 49 when serial. |
| L4 | The compiled-program cache (PB985) makes a re-gate after a test-only fix skip recompilation — never clear it to "be safe". | Cold re-gate 22m53s → warm 1m18s. |
| L5 | Land ONLY via `bash scripts/push-main.sh` (required `ci-gate`); read the CI run; a red is a blocking finding landed alone. | Main red 29 h across 16 runs (PB796). |
| L6 | Verdict batches re-applied with `record_verdicts.py` on the merged tree, never merged as JSON hunks; check the GAP delta. | Per-record drops. |
| L7 | `git add -A -- . ":!.claude/settings.local.json"` then `git reset -q -- STATUS.md`. | A checkpoint reached main (2026-09-02). |
| L8 | DEVLOG number read after the FINAL fetch+rebase; on a non-fast-forward, rebase and renumber. | Parallel landers. |

## Registrar

| # | Practice | Why |
|---|---|---|
| R1 | Grep every lead against `kb/Work/` first; extend, never duplicate; never edit a note an in-flight implementer holds. | Duplicate notes. |
| R2 | Re-probe on your own build ONLY when the lead lacks a runnable repro + code site (P7); otherwise run the given repro once and record the result. | Triple probing. |
| R3 | One note per mechanism (cluster by root cause); each note records its CODE SITES (file:line) for the implementer. | Owner lever 1; P6. |

## Battery

| # | Practice | Why |
|---|---|---|
| B1 | In a DETACHED worktree cut at the batch head, never on main; long legs one at a time; Normal priority. | A battery on main froze the lander ~45 min. |
| B2 | Attribute every red by name; differential flips per case; re-baseline only when the standard licenses it. | Verdict-evidence invariant. |

## Orchestrator

| # | Practice | Why |
|---|---|---|
| O1 | Dispatch specs are RENDERED: `python .claude/skills/workstream/make_dispatch_specs.py <groups.json>`; then `python .claude/skills/workstream/check_practices.py <specs…>` must pass before the Workflow call. | This file. |
| O2 | Fill implementer slots one group per subsystem; land finished work first; one battery per landing batch; ledger refresh after every GAP-moving landing. | SKILL.md §2–§3. |
| O3 | Read the quota meter (Claude in Chrome) before every dispatch; obey the day's allowance and the ~85 % session STOP. | Owner 2026-09-12/20/23. |
