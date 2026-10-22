# 2026-10-22 — The fuzz generator's prompt, its repair loop and its seeds, measured (kb/Work PB3523)

Experiment record (owner standing rule 2026-09-28). The third differential-fuzz record, after
`2026-10-20-differential-fuzz.md` (the harness) and `2026-10-22-differential-fuzz-second-night.md` (the second night,
whose 47 % BOTH_REJECT share opened PB3523). Raw data: `2026-10-22-differential-fuzz-prompt.json` (one row per program
per arm, 240 rows). The runs' own directories were session scratch.

## Question

The owner, 2026-10-22 ~12:05 PDT: "Ideally, we update the prompt to correct COBOL code is generated initially, not
requiring any, or at least fewer, repair attempts." On the second night (run `fuzz-20261021-211424`, 4,931 programs)
both compilers refused 2,326 programs (47 %); every one of them used all three repair rounds, and about 65 % of the
night's generations went to programs that never compiled. The three worst seed families were §13.18 (61 %), §14.9
(58 %) and §8.4 (53 %).

## Hypothesis (the note's, stated before the runs)

The root is the prompt's lack of the clause's syntax: a prompt that carries the seed clause's general format, the
standing rules the refusals show the model lacked, and one compiling example per seed family raises first-attempt
validity; quoting the refused line and stopping a repeated diagnostic early make the repair loop cheaper.

## Design

`differential_fuzz.py --bake-off qwen3.6:35b --count 40 --clauses 13.18,14.9,8.4 --seed 20261022 --jobs 4`, the
generator on the owner's shared RTX 5090, both judges as on the second night (WiseOwl built from origin/main 2850429db,
GnuCOBOL 3.2 `-std=cobol2014` under WSL). The same `--seed` gives the same seed walk, so arms that share a seed list
see the same 14 clauses, three programs each (the 40th program is a fourth clause's first). Measured per arm:
first-attempt validity (usable with no repair round), usable (at least one compiler accepted), BOTH_REJECT, the
generations spent, and ON TOPIC: a usable program whose seed names a clause, statement or function keyword uses that
keyword as a COBOL word outside its literals and hyphenated names (a crude test, but it separates a PAGE-clause program
that writes an RD from one that only DISPLAYs the words "PAGE LIMIT").

## Attempts

| Arm | Harness | Seeds | Ran (PDT) | First attempt | Usable | On topic | BOTH_REJECT | Early stops | Generations |
|---|---|---|---|---|---|---|---|---|---|
| A | origin/main (old prompt) | full walk | 12:24–12:29 | 2/40 | 17/40 | 8/15 | 23 | — | 130 |
| B | this branch: prompt + early stop | full walk | 12:29–12:32 | 1/40 | 6/40 | 3/4 | 34 | 23 | 113 |
| C | origin/main + seed exclusion | declined seeds out | 12:38–12:41 | 10/40 | 23/40 | 16/19 | 17 | — | 110 |
| D | this branch: prompt, early stop, exclusion | declined seeds out | 12:41–12:45 | 10/40 | 27/40 | 23/24 | 13 | 7 | 98 |
| E | D without the early stop | declined seeds out | 12:45–12:52 | 9/40 | 21/40 | 17/19 | 18 | — | 112 |
| F | as landed: D plus the Foreword's 2023 changes in the prompt | declined seeds out | 12:55–12:59 | 8/40 | 22/40 | 17/19 | 18 | 10 | 98 |

Arm B looked like a regression and was diagnosed rather than kept or dropped. Its walk (EXIT, FREE, BLINK, SORT, REPORT,
DYNAMIC LENGTH, reference modification, SET, CONTINUE, CODE-SET, BLANK, PAGE, UNDERLINE, REQUIRED) held four screen
clauses. A screen program compiled by hand drew COBOLNET1560 from WiseOwl, which declines the whole optional
screen-handling module (docs/CONFORMANCE.md §5, A.4.2), so such a seed can only end BOTH_REJECT or as a WE_REJECT the
conformance claim already explains. Arm A's "usable" screen and PAGE programs (five AGREE rows) used no screen or report
construct at all: they DISPLAYed the clause's words. That produced the seed exclusion (`declined_seed_titles`: the whole
clauses and statements of every Annex A.4 module §5 records as Not claimed and no other module lists, 25 titles) and arms C to E on the walk it
gives (BASED, BLOCK CONTAINS, CALL, CLASS, DELETE, EXTERNAL, GO TO, IF, LINAGE-COUNTER, NULL, RENAMES, SIGN, SOURCE,
START).

## Result

- **The prompt did not move first-attempt validity.** On one walk the old prompt wrote 10 of 40 programs usable at the
  first attempt (C) and the new prompt 10, 9 and 8 (D, E, F). D and E differ only in the early stop, which can only
  lower the usable count, yet D has 6 more usable programs: that is the run-to-run noise at 40 programs, and every
  prompt effect measured here is inside it. The new prompt's usable programs stayed on topic as often or more (23/24,
  17/19, 17/19 against C's 16/19; B's 3/4 against A's 8/15): weak evidence, at this size, that the clause's format in
  context keeps the model on the construct under test.
- **The early stop saved 12 % of the generations** (D and F 98, E 112, C 110) with no loss outside the noise. In D, 7
  programs stopped early, in F 10; in B, 23 (its FREE, EXIT, screen and report programs drew the same refusal round
  after round).
- **The declined seeds are 9 % of a night, not its root.** In the second night they wrote 450 of the 4,931 programs, 330
  of the 2,326 BOTH_REJECT rows (14 %) and 35 WE_REJECT rows the claim explains, at 1,613 of the 14,479 generations
  (11 %). Excluding them moves that night's BOTH_REJECT share from 47 % to 45 %. (The bake-off ran with 27 declined
  titles: two of them, the PRESENT WHEN and VARYING clauses, are listed by declined A.4.14 VALIDATE and by claimed A.4.11
  Report Writer alike, and branch review R1-1 returned them to the seeds. Neither was on any arm's walk, so no arm's
  figures change; on the second night their 36 programs drew 5 BOTH_REJECT and 5 WE_REJECT rows.) B's walk drew four of them in its first
  fourteen seeds, which is why the A-to-D comparison overstates the change (BOTH_REJECT 23 of 40 to 13 of 40).
- What the remaining refusals show (B, D, E rounds): `ALLOCATE … GIVING` in a FREE program, EXIT PERFORM outside an inline
  PERFORM, SET condition-name TO FALSE with no WHEN SET TO FALSE, a REPORT SECTION before WORKING-STORAGE, an
  implementor-defined physical-structure name in DYNAMIC LENGTH, SORT without an SD file. These are a 35-billion-parameter
  model's errors on constructs it does not know, each against a rule the clause text it was given states.

## Limits

Forty programs an arm at temperature 0.3; one walk per comparison; the on-topic test is a keyword test. The
first-attempt numbers cannot distinguish two prompts whose effect is under about 15 points.

## Decision

Kept (kb/Work PB3523's six items): the structural clause text within `CLAUSE_BUDGET`, the per-family examples (each AGREE
on both judges), the standing rules (with the 2023 additions read from Annex E.2 item 25 and the Foreword's change list),
the refused line quoted in a correction, `repair_rounds`, the early stop, the
first-attempt measure in the report and the bake-off table, and the run cadence (a `--model` run refuses to start while
a fuzz-tagged compiler-defect note is open, the owner's rule of 12:15 PDT). Added: the declined-seed exclusion. Not
claimed: that the prompt fixes first-attempt validity; the measurement says it does not, at this size. The next levers
are leads in the PB3523 report (thinking mode for the generator, seeds weighted by their measured yield).

## Links

kb/Work PB3523; the branch's DEVLOG entry; `scripts/differential_fuzz.py` (`seed_text`, `system_prompt`, `PromptFacts`,
`declined_seed_titles`, `refusal_key`, `cadence_gate`).
