# Spec reconciliation — durable findings ledger

Agent findings from the PDF-vs-markdown reconciliation land here, **one file per agent, written before the agent
returns**. That is the point of this directory: a rate limit, a session limit or a stopped workflow must not lose
work that has already been done. A finding that exists only in a workflow return value is gone the moment the run
is interrupted, and re-deriving it costs another full page render and read.

| file | written by | holds |
|---|---|---|
| `compare-p<first>-p<last>.json` | a Compare agent | its batch, the pages it checked, and every claim |
| `verify-p<page>-<kind>-<n>.json` | a Verify agent | the claim **and** the adversarial verdict, standing alone |
| `LEDGER.json` / `REPORT.md` | `scripts/spec/merge_reconciliation.py` | the merge of whatever exists |

A `compare-*.json` with `findings: []` is meaningful — it is evidence that a batch was swept and found clean. Its
**absence** is indistinguishable from work that never ran, which is why agents write it either way.

## Reading progress, including mid-run

```
python scripts/spec/merge_reconciliation.py --expect 173
```

Safe to run while agents are still working. It reports partial coverage rather than failing, names the pages not
yet swept, and separates claims into confirmed / refuted / **unverified**. Unverified claims are not actionable —
a claim without an adversarial verdict is a hypothesis, and acting on one risks "fixing" correct text.

## Re-running only what is missing

The page list is an argument, so pass just the gap:

```
Workflow({ scriptPath: ".claude/workflows/spec-reconcile.js", args: [<the unswept pages>] })
```

Nothing already on disk is recomputed.

---

## STATUS: sweep COMPLETE — repairs pending

All 1,261 pages reconciled, every claim adjudicated: **346 claims · 210 confirmed · 136 refuted · 0 unverified**
(41 normative · 123 structural · 46 cosmetic).

**The repair order is `REPAIR-PLAN.md` in this directory. All 210 are in scope — cosmetic included.**

## Superseded resume point (paused 2026-07-26 19:56 PDT, resolved 20:24)

**Sweep coverage is COMPLETE — all 1,261 pages compared against the canonical PDF.** Nothing needs re-sweeping.

State at pause: **346 claims · 159 confirmed** (41 normative · 85 structural · 33 cosmetic) · **104 refuted** ·
**83 unverified**.

The 83 unverified are chunk-3 claims whose adversarial verify agents were stopped mid-flight. They are
**NOT actionable** — a claim without a verdict is a hypothesis, and acting on one risks "fixing" correct text.
Their pages are listed in `UNVERIFIED-PAGES.txt`.

### To resume, in order

1. **Finish the verification.** Replays completed compare agents from cache and re-runs only the missing verifiers:

   ```
   Workflow({ scriptPath: ".claude/workflows/spec-reconcile.js",
              resumeFromRunId: "wf_437b4170-d19",
              args: "933-961,963-1176,1178-1261" })
   ```

2. **Re-merge and confirm zero unverified:**

   ```
   python scripts/spec/merge_reconciliation.py --expect 1261
   ```

3. **Publish the tracker section** (only once `unverified` reads 0 — the generator refuses to look final while
   pages remain unswept, but it cannot tell that a verdict is missing):

   ```
   python scripts/spec/tracker_section.py
   ```

4. **Then repairs**, by DEFECT FAMILY — never per finding. The seven ON/OFF directive notes must agree with each
   other or the inconsistency merely moves. After each family: re-run `python scripts/spec/extract_rule_catalog.py`
   and confirm the page-anchor count is still 1261.

5. **Then the grammar audit** — `.claude/workflows/spec-grammar-conformance.js`, §14 first (489 items). Remember a
   diagram defect is TWO items: the transcription repair here, and a fix-queue COMPILER bug wherever the grammar
   inherited it (proven on GOBACK).
