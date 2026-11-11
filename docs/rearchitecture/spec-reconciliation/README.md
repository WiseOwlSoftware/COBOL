# Spec reconciliation — durable findings ledger

Agent findings from the PDF-vs-markdown reconciliation land here, **one file per agent, written before the agent
returns**. That is the point of this directory: a rate limit, a session limit or a stopped workflow must not lose
work that has already been done. A finding that exists only in a workflow return value is gone the moment the run
is interrupted, and re-deriving it costs another full page render and read.

| file | written by | holds |
|---|---|---|
| `agents/compare-p<first>-p<last>.json` | a 2023 Compare agent | its batch, the pages it checked, and every claim |
| `agents/verify-p<page>-<kind>-<n>.json` | a 2023 Verify agent | the claim **and** the adversarial verdict, standing alone |
| `LEDGER.json` / `REPORT.md` | `scripts/spec/merge_reconciliation.py --edition 2023` | the 2023 merge: confirmed, refuted and unverified claims (the shape `spec-grammar-impact.js` reads) |
| `2002/compare-*.json`, `2002/verify-*.json` | the 2002 reconciliation's Compare and Verify agents (kb/Work PB4813, PB5255) | the same per-agent files, in the 2002 edition's own directory |
| `2002/ranges.json` | `scripts/spec/reconcile_ranges.py --edition 2002` | the 2002 page-range plan: 882 pages in batches of 12, each marked `figure`, `ambiguous` and `clauses`, flagged and figure batches first, two halves of equal estimated work (`account1`, `account2`), each cut into runs of six batches; generated and `--check`ed, never typed |
| `LEDGER-2002.json` / `2002/REPORT.md` | `scripts/spec/merge_reconciliation.py --edition 2002` | the 2002 merge, in `LEDGER-1985.json`'s shape (`edition`, `about`, `pages`, `claims`, `sweeps`, `boxes`, plus `pages_swept`): each claim with its verdict as `batch` (CONFIRMED, REFUTED, UNVERIFIED) and its decision; a re-merge keeps every decision and sweep |
| `LEDGER-1985.json` | the 1985 reconciliation (kb/Work PB4850) | every claim of the 1985 batch passes with its decision (`APPLIED` with its edits, `REFUTED`, `ALREADY`, `FORMAT`, `BOX`, `FIGURE`, `KEPT`), the sweeps of each repair's siblings, and every level-2 box (`MARKED` with its blocks, `FORMAT`, `NOT MARKED`); kept apart from 2023's `LEDGER.json`, and held to the transcription by `verify_publishable.py --edition 1985` (TRANSCRIPTION-STATE.md "The 1985 transcription") |
| `figures-<edition>/p<page>-<figure>.json` | a figure refuter (a scanned edition, kb/Work PB4849) | its verdict on the `figure`-th `<pre>` block (from 0) of PDF page `page` (the key), `drawn_lines` (the block's lines as drawn, copied from `sweep_figures.py --cross-check`'s ledger `drawn`: the identity, so a verdict whose ordinal a `<pre>` added or removed earlier on the page shifted onto another block is refused, never applied to it), `decided` (`generated`, `drawn`, `neither`, or `not-a-format`), `figure_lines` (the decided figure drawn by FIGURE-STYLE.md, for `drawn` the batch's drawing redrawn by `figure_reading.restyle`; none for `not-a-format`) and `reason` (what the printed page shows). `sweep_figures.py --edition <edition> --apply` writes it; `load_verdicts` refuses a malformed one |

A `compare-*.json` with `findings: []` is meaningful — it is evidence that a batch was swept and found clean. Its
**absence** is indistinguishable from work that never ran, which is why agents write it either way.

## Reading progress, including mid-run

```
python scripts/spec/merge_reconciliation.py --edition 2002 --expect 882
```

Safe to run while agents are still working. It reports partial coverage rather than failing, counts the pages
swept against `--expect`, and separates claims into confirmed / refuted / **unverified**. Unverified claims are not
actionable — a claim without an adversarial verdict is a hypothesis, and acting on one risks "fixing" correct text.
It merges only the edition's own directory (`agents/` for 2023, `2002/` for 2002); 1985 has no row, because its
ledger came from the Message Batches passes and a merge would overwrite its decisions.

## Running a 2002 range (kb/Work PB5255)

The decisions (the project owner's controller, 2026-11-11, kb/Work PB4813's addendum `2026-11-11-reconcile-route.md`):
the 2002 reconciliation runs through this Workflow on the plan's quota, never Message Batches; its agents are Opus at
effort medium; all 882 pages are reconciled, the flagged batches (the 57 ambiguous pages, §2002:11.1.1 and
§2002:13.14.1) and the figure batches first; every claim is verified by its own adversarial agent; the refuter pass
over every drawn figure is the campaign's second phase.

1. **Make a branch and a worktree for the run.** The agents write into `outDir`, the 2002 directory of THAT
   worktree, and build their work units from that worktree's transcription; the branch lands the files.
2. **Pick the next run** from the plan, never a typed list:
   ```
   python scripts/spec/reconcile_ranges.py --edition 2002 --check
   python -c "import json; d=json.load(open('docs/rearchitecture/spec-reconciliation/2002/ranges.json', encoding='utf-8')); print([r['run'] + ' ' + r['pages'] for r in d['halves']['account1']['runs']])"
   ```
   Account 1 runs the `account1` half's runs in order, account 2 the `account2` half's; the halves are of equal
   estimated work and each starts with its flagged and figure batches.
3. **Call the Workflow** with every argument (each is required; a missing or unknown one stops the run before any
   agent starts). The first run of `account1`, from a worktree at `E:/COBOL/.claude/worktrees/<branch>`:
   ```
   Workflow({ scriptPath: ".claude/workflows/spec-reconcile.js", args: {
     pages: "1-12,37-48,85-96,157-168,193-204,253-264", edition: 2002, model: "opus", effort: "medium",
     outDir: "E:/COBOL/.claude/worktrees/<branch>/docs/rearchitecture/spec-reconciliation/2002",
     workDir: "E:/Temp/specrecon-2002-account1-r01" } })
   ```
   `workDir` is the run's own scratch directory (name it after the run), so two fleets on one machine never share
   their renders. A gap in `pages` starts a new batch, so each batch of the plan is one Compare agent and its file is
   `compare-p<first>-p<last>.json` after the plan's batch.
4. **Merge and land.** `python scripts/spec/merge_reconciliation.py --edition 2002 --expect <pages run so far>` writes
   `LEDGER-2002.json` and `2002/REPORT.md`; commit them with the agent files on the run's branch and land it.

## Re-running only what is missing

The page list is an argument, so pass just the gap (the other arguments as above):

```
Workflow({ scriptPath: ".claude/workflows/spec-reconcile.js", args: { pages: "<the unswept pages>", edition: 2002, model: "opus", effort: "medium", outDir: "<worktree>/docs/rearchitecture/spec-reconciliation/2002", workDir: "<this run's scratch>" } })
```

Nothing already on disk is recomputed.

---

## 2002 STATUS: tooling ready, no page reconciled yet

The Workflow, the merge and the page-range plan take the 2002 edition (kb/Work PB5255); 0 of 882 pages have a
recorded result. `LEDGER-2002.json` joins `verify_publishable.LEDGER` once every claim in it is decided.

## 2023 STATUS: sweep COMPLETE — repairs pending

All 1,261 pages reconciled, every claim adjudicated: **346 claims · 210 confirmed · 136 refuted · 0 unverified**
(41 normative · 123 structural · 46 cosmetic).

**The repair order is `REPAIR-PLAN.md` in this directory. All 210 are in scope — cosmetic included.**

## What the 2023 reconciliation established

Coverage is COMPLETE — all 1,261 pages were compared against the canonical PDF and every claim adjudicated.
`LEDGER.json` is the SSOT for the findings; `REPAIR-PLAN.md` is the order and the mechanism.

⛔ The **FIGURE** classes are now closed by CONSTRUCTION rather than by repair: every general format is
generated from the printed page by `scripts/spec/render_figure.py` and written by `sweep_figures.py`, whose
`--check` is the standing regression gate. Do not hand-repair a figure — regenerate it.

