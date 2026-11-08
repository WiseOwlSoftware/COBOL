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
| `LEDGER-1985.json` | the 1985 reconciliation (kb/Work PB4850) | every claim of the 1985 batch passes with its decision (`APPLIED` with its edits, `REFUTED`, `ALREADY`, `FORMAT`, `BOX`, `FIGURE`, `KEPT`), the sweeps of each repair's siblings, and every level-2 box (`MARKED` with its blocks, `FORMAT`, `NOT MARKED`); kept apart from 2023's `LEDGER.json`, and held to the transcription by `verify_publishable.py --edition 1985` (TRANSCRIPTION-STATE.md "The 1985 transcription") |
| `figures-<edition>/p<page>-<figure>.json` | a figure refuter (a scanned edition, kb/Work PB4849) | its verdict on the `figure`-th `<pre>` block (from 0) of PDF page `page` (the key), `drawn_lines` (the block's lines as drawn, copied from `sweep_figures.py --cross-check`'s ledger `drawn`: the identity, so a verdict whose ordinal a `<pre>` added or removed earlier on the page shifted onto another block is refused, never applied to it), `decided` (`generated`, `drawn`, `neither`, or `not-a-format`), `figure_lines` (the decided figure drawn by FIGURE-STYLE.md, for `drawn` the batch's drawing redrawn by `figure_reading.restyle`; none for `not-a-format`) and `reason` (what the printed page shows). `sweep_figures.py --edition <edition> --apply` writes it; `load_verdicts` refuses a malformed one |

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

## What the reconciliation established

Coverage is COMPLETE — all 1,261 pages were compared against the canonical PDF and every claim adjudicated.
`LEDGER.json` is the SSOT for the findings; `REPAIR-PLAN.md` is the order and the mechanism.

⛔ The **FIGURE** classes are now closed by CONSTRUCTION rather than by repair: every general format is
generated from the printed page by `scripts/spec/render_figure.py` and written by `sweep_figures.py`, whose
`--check` is the standing regression gate. Do not hand-repair a figure — regenerate it.

