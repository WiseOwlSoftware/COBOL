---
name: workstream
description: Use BEFORE dispatching any fleet, lander, implementer or adjudication workflow - the owner's standing instructions (2026-09-02) for running workstreams so a session-limit kill costs at most one step and a restart never repeats work - checkpoint to disk, fresh agents from checkpoints, a hard concurrency budget, finished work landed first, central id allocation. Carries the brief and workflow templates.
---

# Workstream — token-frugal, restart-safe orchestration

> ⛔ **Owner standing instruction (2026-09-02).** Twenty-eight concurrent Opus agents burned ~20% of a session window
> in eleven minutes; ~fifty exhausted a window in ~2.5 h; two cutoffs in one day (the reset hour is NOT fixed — read it
> off the 429). A RESUMED long transcript re-reads its whole context on every turn, so resuming eight 300-turn
> implementers was the most expensive thing run all day, and un-checkpointed refuters lost every rule they had decided.
> "Run all this work so that we don't repeat effort when hitting a limit and resuming."

The orchestrator (the session model) dispatches, reconciles, gates and commits; every other job — probe, implement,
validate, adversarial review, land — is a subagent on the model `~/.claude/settings.json` names (`claude-opus-5`),
passed explicitly as `model: 'opus'`.

## 1. Checkpoint to disk, never to a transcript

| job | checkpoint | resume unit |
|---|---|---|
| implementer (worktree) | `git commit -m "WIP checkpoint: …"` on the worktree branch **after every mechanism and every gate**, plus `<worktree>/STATUS.md` with `DONE` (per mechanism) · `NEXT` (the exact next step) · `BLOCKED` · `GATE` (last verdict lines + filter) · batch-file path · codes used | one mechanism |
| workflow stage (adjudicate / refute / write / validate) | one JSON line per rule (or draft) appended to `<out>/<stage>-<slug>.jsonl` the moment it is decided; read-and-skip on start; the stage's whole result also written to `<out>/out-<slug>.json` | one rule |
| lander | a commit in ITS worktree after each numbered step + `STATUS.md` | one step |
| orchestrator | reports are FILES under the scratchpad (`reports/<cluster>-report.md`), briefs are FILES referenced by path; the conversation carries only pointers | — |

**A killed agent is replaced by a FRESH agent that reads the checkpoint** (`STATUS.md` + `git log`, or the `.jsonl`).
Resume via `SendMessage` only when the agent is within a step of finishing. A workflow resumes with `resumeFromRunId`,
but design its stages to read inputs from disk (`out-<slug>.json`) so a rewritten script never re-runs completed work.

## 2. Concurrency budget

**1 lander + ≤3 implementers + one 4-wide read-only chunk (≈8 agents).** Workflows take `concurrency`/
`refuteConcurrency` args and run chunks with `parallel()` inside a loop, never a 16-wide `pipeline`. A fleet over ~40
agents or an implementer over ~100 turns is the signal to SPLIT the work, not to wait. Stage the earliest-stage, largest
jobs behind the near-done ones.

## 3. Landing order and mechanics

- **Land finished work first**; read-only fleets are staged behind landings. **One lander on main at a time** (DEVLOG
  numbering and fast-forward ordering); no lander carries more than ~3 landings in one transcript — split the queue into
  fresh agents.
- A worktree-isolated agent cannot run git against the shared checkout (the harness refuses `-C`, `cd`, EnterWorktree):
  landers gate in THEIR worktree, then `git fetch origin && git rebase origin/main && git push origin HEAD:main`
  (fast-forward, never `--force`); the orchestrator runs `git merge --ff-only origin/main` locally and removes dead
  worktrees itself (`git worktree remove --force`, `git branch -D`).
- Verdict batches are RE-APPLIED on the merged tree (`record_verdicts.py`), never merged as inventory JSON hunks.
- Read-only fleets probe a **pinned worktree with its own built compiler** (`git worktree add --detach <path> <sha>`,
  build there once) so no landing swaps a binary under them.
- One comprehensive battery per landing batch, run by the orchestrator when no fleet is live.
- `python scripts/semgrep/verify.py` has a recorded red baseline (`kb/Work/PB175`): a landing must not INCREASE it.

## 4. Central allocation

The orchestrator allocates `kb/Work` ids and diagnostic-code ranges in the dispatch brief; agents never pick their own
(five collisions in one day each cost a renumbering pass). DEVLOG numbers are read from the file at landing time
(top entry + 1). `session-probe` reports the next free diagnostic code — it must end above ALL codes claimed by
in-flight worktrees.

## 5. On restart after a cutoff

1. Read the reset time off the 429 message; check `date` against it.
2. `git status` on main — a dead lander may have applied its patch (finish from the NEXT step; never re-apply).
3. `git worktree list` + dirt per worktree (`git -C <wt> status --short -- . ':!.claude/settings.local.json'`) +
   `STATUS.md` per worktree → who is near done.
4. Dispatch FRESH agents from checkpoints in landing order; resume in place only the near-done ones.

## 6. Templates (substitute the session's paths for `{SCRATCH}`, `{PIN}`)

`templates/` beside this file: `implementer-brief.md` · `lander-brief.md` · `golden-lander-brief.md` ·
`registrar-brief.md` · `wf_lane3_adjudicate.js` · `wf_lane3_refute.js` · `wf_lane1_draft.js` ·
`wf_lane1_validate.js` · `merge_batch.py` · `build_lane1_inputs.py`. Each carries the checkpoint protocol already;
dispatch by pointing the agent at the file plus the substitutions, never by pasting the brief into the prompt.
