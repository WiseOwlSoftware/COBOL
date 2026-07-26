---
name: session-start
description: Use at the start of every COBOL.NET session, and whenever asked "where are we", "what is next", or "what is the current state" - reads the one live-state SSOT, runs the mechanical probe, and confirms the gate baseline before any code change.
---

# Session start

Live state is **computed, never remembered**. Memory and any status line written in a doc may be stale; these three
steps are not.

## 1. Read plan §0 — the ONLY live-state SSOT

`docs/COBOLNET_REARCHITECTURE_PLAN.md` §0. It owns: where we are, the NEXT worklist in order, the gates, the open
GAPs, and the standing facts. **Trust §0 over any status written anywhere else, including memory and this skill.**

## 2. Run the probe — never hand-read what a script computes

```
pwsh -NoProfile -File scripts/session-probe.ps1
```

Reports branch · dirty/unpushed · next-free diagnostic code · VCR todo count · corpus counts · inventory GAP.

Reading its output:
- **`⚠ UNEXPECTED BRANCH`** — expected is `phase-14` or `main`. Stop and ask before working on another.
- **`⚠ DIRTY TREE`** — commit or explain before proceeding. Never start new work on top of unexplained changes.
- **`diag`** — claim the next diagnostic code from `next free`, never by reading a list. `src-grep max >= catalog max`
  is the expected steady state; **catalog above src is the real anomaly** (an orphan descriptor) — reconcile it
  before allocating.
- **`invent`** — the traceability inventory GAP count is the P14 burn-down metric. "not built yet" means the
  denominator does not exist and no conformance percentage can be quoted.

## 3. Confirm the battery baseline before changing code

Plan §0 "Gates" carries the last known-green counts. If the last session ended mid-batch, re-confirm green before
building on it — see the `gate` skill.

## Before the session ends

Update §0 (it is the single-write location for live state) and add a `DEVLOG.md` entry per commit. Commit and push
every checkpoint.

## What NOT to do here

- Do not summarize the plan back to the owner unless asked. Read it and start working.
- Do not ask "what would you like to work on?" — the top of §0 NEXT is the answer.
