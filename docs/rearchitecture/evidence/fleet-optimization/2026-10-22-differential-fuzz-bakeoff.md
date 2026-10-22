# 2026-10-22 — The five-model fuzz bake-off on the new prompt

Experiment record (owner standing rule 2026-09-28). The fourth differential-fuzz record, after
`2026-10-22-differential-fuzz-prompt.md` (the prompt, repair loop and seeds, kb/Work PB3523), whose harness this bake-off
runs. The harness's own output is `2026-10-22-differential-fuzz-bakeoff/report.md` (copied unchanged; it holds no
GnuCOBOL source and no licence text: `grep -c GPL` prints 0).

## Question

The owner, 2026-10-22 ~13:20 PDT: "We should do a new bake off after we have the enhanced fuzz process in place". At
~13:25 PDT: "Pull nemotron-3.5-bolt and qwen3.8:27b for the bake off too". Which local model writes the most valid COBOL at
the first attempt (the owner's goal in the PB3523 record) and which gives the most usable programs after repairs?

## Design

Run by the Mythos session, 14:4x to 15:0x PDT, from the PB3523 branch's worktree at its landing head (the same content as
PR #398, now main 4ea4be06a):

`differential_fuzz.py --bake-off qwen3.6:35b,qwen3-coder:30b,laguna-xs-2.1,nemotron-3.5-bolt,qwen3.8:27b --count 20 --jobs 2 --seed 20261023 --cobol <that worktree's cobol.exe> --out E:/COBOL-coord/scratch/bakeoff-20261022`

- Judge: main's compiler with all four second-night defects fixed (PB3510, PB3511, PB3512, PB3517), and GnuCOBOL 3.2
  as before.
- Seeds: 233 seed clauses under 14.9, 15, 13.18 and 8.8 (25 declined-module seed titles excluded). Same seed for every
  model, so each model sees the same seed walk. Temperature 0.3, 3 repair rounds.
- The generator is the owner's shared GPU.

## Result (the harness's table, verbatim)

| Model | Programs | Usable | Usable on the first attempt | Model failures (both refuse, no program, or both hang) | GnuCOBOL alone refuses | Median model s | Lead rows |
|---|---|---|---|---|---|---|---|
| qwen3.6:35b | 20 | 19/20 (95%) | 5/20 (25%) | 1 | 1 | 4 | 4 |
| qwen3-coder:30b | 20 | 4/20 (20%) | 2/20 (10%) | 16 | 0 | 13 | 0 |
| laguna-xs-2.1 | 20 | 15/20 (75%) | 7/20 (35%) | 5 | 0 | 7 | 6 |
| nemotron-3.5-bolt | 20 | 16/20 (80%) | 12/20 (60%) | 4 | 2 | 5 | 7 |
| qwen3.8:27b | 20 | 11/20 (55%) | 2/20 (10%) | 9 | 1 | 30 | 1 |

Each model's own `report.md` headline (the per-model run directories were session scratch):

- qwen3.6:35b: usable 19 (14 of them after a repair round); first attempt 5; the model's failures 1 (1 ended early on a
  repeated diagnostic); GnuCOBOL alone refused 1; median 4 s. Classes: AGREE 15, WE_REJECT 3, THEY_REJECT 1, BOTH_REJECT 1.
- qwen3-coder:30b: usable 4 (2 after a repair round); first attempt 2; failures 16 (10 ended early on a repeated
  diagnostic); GnuCOBOL alone refused 0; median 13 s. Classes: BOTH_REJECT 16, AGREE 4.
- laguna-xs-2.1: usable 15 (8 after a repair round); first attempt 7; failures 5 (2 ended early); GnuCOBOL alone refused
  0; median 7 s. Classes: AGREE 9, BOTH_REJECT 3, OUTPUT_DIFFERS 3, WE_REJECT 3, NO_PROGRAM 2.
- nemotron-3.5-bolt: usable 16 (4 after a repair round); first attempt 12; failures 4 (4 ended early); GnuCOBOL alone
  refused 2; median 5 s. Classes: AGREE 9, WE_REJECT 4, BOTH_REJECT 4, THEY_REJECT 2, OUTPUT_DIFFERS 1.
- qwen3.8:27b: usable 11 (9 after a repair round); first attempt 2; failures 9 (7 ended early); GnuCOBOL alone refused 1;
  median 30 s. Classes: AGREE 10, BOTH_REJECT 7, NO_PROGRAM 2, THEY_REJECT 1.

The lead rows (survivors) are in the raw report and are the next triage pass's input; none was triaged for this record.

## Reading

- On first-attempt validity, the owner's stated goal, nemotron-3.5-bolt wrote 12 of 20 programs usable with no repair,
  against qwen3.6:35b (last night's model) 5 of 20, laguna-xs-2.1 7 of 20, qwen3.8:27b 2 of 20 and qwen3-coder:30b 2 of 20.
- On usable after repairs, qwen3.6:35b leads with 19 of 20; nemotron-3.5-bolt has 16, laguna-xs-2.1 15, qwen3.8:27b 11,
  qwen3-coder:30b 4.
- Noise: at 20 programs, two arms that differed only by the early stop differed by 6 usable programs in the PB3523
  record. The first-attempt gap between nemotron-3.5-bolt and the next model (7 programs, against laguna-xs-2.1) is at
  the edge of that noise, and the total-usable gap between qwen3.6:35b and nemotron-3.5-bolt (3 programs) is inside it.
- qwen3.8:27b was the slowest (median 30 s a program) and qwen3-coder:30b refused 16 of 20.

## Decision

The model for the next overnight run was the owner's decision. At about 15:12 PDT on 2026-10-22 the owner answered "yes"
to the question "Should tonight's run use nemotron-3.5-bolt?". The third overnight run therefore uses
nemotron-3.5-bolt, with the judge `E:\tmp\fuzz-judge-20261022\cobol.exe` (the compiler of main 4ea4be06a), seed
20261024, 8 hours and 4 jobs.

## Links

kb/Work PB3523; `2026-10-22-differential-fuzz-prompt.md`; `2026-10-22-differential-fuzz-bakeoff/report.md`;
`scripts/differential_fuzz.py` (`--bake-off`).
