# 2026-10-27 — Sonnet against Opus implementers: weekly points per landed note, rework charged in

Experiment record (owner standing rule 2026-09-28: every fleet experiment goes here). The decision it supports is kb/Work
PB3868. Raw data beside this file in `2026-10-27-implementer-model-cost/`: `results.csv` (one row per group) and the
scripts that built it (`extract.py`, `analyze.py`, `stats.py`, `stats2.py`, `stats3.py`; they read the session transcripts
under the Claude config directory, which may since have been pruned).

**The question (owner, 2026-10-27):** make `cobol-implementer` run a cheaper model, but "Ensure that any change is
cost-effective. A cheaper model isn't cost-effective if it fails." So the measure is weekly quota points per LANDED note,
with every review, fix round, finisher and later retry the implementer's output caused charged to it.

## Method

- Every Workflow transcript directory from 2026-09-27 to 2026-10-27 (251 directories, 1,060 implementer agents, 789
  groups). The model is read from each assistant message (`workflow_metrics.py` does not report it).
- Points: input + output + cache-creation tokens divided by `model_rules.json` `calibration.tokens_per_point`
  (Opus 1.6 M, Sonnet 4.0 M per point), per message.
- A group is its first implementer plus its branch reviews, fix rounds and finisher. A later re-dispatch of the same
  lead note is a retry, charged back to the first attempt.
- Landed: the group's branch head is an ancestor of `origin/main`. That test holds only from about 2026-10-17, when the
  merge queue began; before it trains cherry-picked, so the landing comparison uses 10-17 onward.

## Results

| From 2026-10-17 | Sonnet (n=206) | Opus (n=106) |
|---|---|---|
| Implementer points per group, mean / median | 0.073 / 0.061 | 0.242 / 0.234 |
| All points per group (reviews, fixes, finisher) | 0.193 | 0.446 |
| DONE on the first pass | 92 % | 82 % |
| DONE with no review fix round | 45 % | 35 % |
| Finisher needed | 3 % | 3 % |
| Landed | 95 % | 94 % |
| Re-dispatched later | 6 % | 15 % |
| Points per landed note, retries charged in | **0.16** | **0.27** |

Over the whole month (landing not measurable before 10-17): Sonnet 379 groups, 0.150 points per group, 0.11 per landed
note; Opus 410 groups, 0.296 per group, 0.17 per landed note. Retries (attempt-1 groups from 10-07): Sonnet 34 of 283
re-dispatched (12 %), the retries going 22 to Sonnet and 26 to Opus; Opus 37 of 222 (17 %).

**Like for like.** Two areas have groups of both since 10-17. `runtime/io` (Sonnet 35 groups, Opus 19): Sonnet DONE 97 %,
landed 100 %, 0.16 points per note; Opus DONE 84 %, landed 95 %, 0.21 per note. `tests/conformance-suite` (Sonnet 5, Opus
6) goes the OTHER way: Sonnet 0.165 points per note against Opus 0.138 (0.165 against 0.146 with retries charged in). From
10-07 Sonnet is cheaper per note in 12 of 13 shared areas by the implementer-and-rework cost, and in 9 of 13 with each
lead note's later retries charged back; the cells hold 3 to 13 groups each, so no one area decides.

## Confounds

- **Difficulty.** The planner routes the hard groups to Opus (`routing.opus_when`): since 10-17 its groups include 11
  BLOCKER and 8 grammar groups, while 131 of Sonnet's 206 are MINOR, and its groups average 1.71 notes against 1.29. So
  the table compares Sonnet on routine work with Opus on harder work; the `runtime/io` cell points the same way.
- **Unmeasured:** Sonnet on grammar, OO and CI/build work. It never ran there, so the routing that sends that work to
  Opus has no evidence against it and stays.
- **Not counted:** landing-lane runs, a merge-queue bounce fixed by a finisher outside the group's workflow, and the
  orchestrator's time. The reviews run Opus in both arms, a fixed cost of about 0.10 points a group.
- "Notes landed" counts a note whose status is now landed or retired.

## Reproduce

The scripts read their inputs from their own directory. From a checkout at the measured `origin/main`:

1. `python extract.py` walks the Workflow transcript directories under the Claude config directory and writes
   `all_agents.json` (every agent: label, model per message, counted tokens, result).
2. `git log origin/main --format='%H|%cI|%P|%s' > gitlog.txt` and `git rev-list origin/main > mainshas.txt` (this record:
   origin/main deae52c90, 8,539 commits).
3. `python analyze.py` joins them with `kb/Work` and writes `rows.json` and `results.csv`; `stats.py`, `stats2.py` and
   `stats3.py` print the tables from `rows.json`.

`all_agents.json`, `rows.json` and the two git listings are not committed (8 MB, and derivable); `results.csv` is the
committed result. The transcripts may be pruned, so a re-measure covers the waves since the last one.

## Decision

Sonnet becomes the implementer's default in its one home, the `model` of `.claude/agents/cobol-implementer.md`; the
planner routes to Opus only by `routing.opus_when`. Re-measure with the same scripts after a week of waves, and add a
Sonnet sample on a routed area only by an explicit trial.
