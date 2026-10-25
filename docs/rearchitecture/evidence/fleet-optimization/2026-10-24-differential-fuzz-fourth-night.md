# 2026-10-24 — The fourth overnight differential fuzz run, triaged by twelve Opus adjudicator slices

Experiment record (owner standing rule 2026-09-28: every fleet experiment goes here). Predecessors:
`2026-10-20-differential-fuzz.md` (the harness, kb/Work PB3463), `2026-10-21-differential-fuzz-overnight.md`,
`2026-10-22-differential-fuzz-second-night.md` and `2026-10-23-differential-fuzz-third-night.md`. Raw data:
`E:\COBOL\conformance-suite\results\fuzz-20261023-204437\` (`run.json`, `results.jsonl`, `report.md`, `programs/`). The
triage scripts, the adjudicators' reports and their verdict files are session scratch (`fuzz4\` in session 0a5c7c77's
scratchpad) and are described inline. Nothing here changed a golden or the compiler.

**The owner's instruction** (2026-10-24 ~17:00 PDT): "Also, run the fuzz analysis and report its findings. Do not begin
fixing any fuzz identified issues without my explicit approval". So every finding is filed with `status: owner`
(PB3664 to PB3666), which no lane plans.

## The run

| | |
|---|---|
| Started | 2026-10-23 20:44 PDT (`run.json` `started` 03:44:37 UTC), `--hours 8 --jobs 4`, seed 20261025 |
| Generator | `qwen3.6:35b`, temperature 0.3, 3 variations, up to 3 repair rounds |
| Seeds | 235 clauses of the prefixes `14.9`, `15`, `13.18`, `8.8` (23 declined seeds) |
| Judges | WiseOwl `--std 2014` from `E:\tmp\fuzz-judge-20261023\cobol.exe`; GnuCOBOL 3.2 `-std=cobol2014` under WSL |
| Programs | 5,247 written; 5,246 judged; median model time 13 s a program |
| Usable (both compiled) | 3,229 (62 % of judged), 1,621 of them after a repair round; 1,608 (31 %) usable on the first attempt |
| The model's failures | 2,017 (1,473 ended early on a repeated diagnostic) |

Classes (`report.md`): `BOTH_REJECT` 2,008 · `AGREE` 1,816 · `WE_REJECT` 811 · `OUTPUT_DIFFERS` 424 · `THEY_REJECT` 157 ·
`THEY_FAIL_RUN` 15 · `NO_PROGRAM` 6 · `WE_HANG` 3 · `BOTH_HANG` 3 · `EXIT_DIFFERS` 2 · `RUNNER_ERROR` 1 · `THEY_HANG` 1.

## Triage method (the third night's, with two changes)

The fresh judge was origin/main 300fd78bd, built in the detached worktree `E:\COBOL-wt\fuzz-triage-20261024`.

- **Re-judge.** The 441 `OUTPUT_DIFFERS`, `EXIT_DIFFERS` and `THEY_FAIL_RUN` rows were re-run on the fresh judge and
  rescored with the harness's normalization. That left 405 `OUTPUT_DIFFERS`, 34 `AGREE` and 2 `EXIT_DIFFERS`, for a
  residue of 407 rows in 100 signatures.
- **New: the refusal rows were re-judged too.** All 811 `WE_REJECT` and 3 `WE_HANG` programs were re-compiled on the
  fresh judge. Every one still refused or hung, so none was fixed between the run's judge and main. Each `WE_REJECT`
  section carried a `re-judge now` line, and the vocabulary gained `FIXED-SINCE`, which no slice needed.
- **Slicing.** Twelve slices: A1 (§8.x output differences, 15 signatures), B1 to B4 (the other output differences, 85),
  C1 to C4 (`WE_REJECT`, 83 signatures and 744 rows, the same selection rule as before: every signature of 3 rows or more,
  plus each smaller one whose codes appear in no larger one; 67 rows in small repeat-code signatures were not sliced),
  D1 and D2 (`THEY_REJECT`, `THEY_FAIL_RUN`, `THEY_HANG`, `BOTH_HANG`, 48 sections and 176 rows) and N1 (hang, exit,
  runner and no-program, 12 rows).
- **New: a rolling pool.** One Workflow ran the twelve read-only Opus `cobol-adjudicator` agents four at a time from one
  queue, so a free slot was refilled at once. Each agent decided one verdict per signature, ran `cite.py --check` on every
  citation, appended each verdict to a checkpoint file when it decided it, and returned its report as text. The operator
  re-ran both defects on the fresh judge and re-checked every citation before filing.
- **Cost.** 2,031,074 subagent tokens, 846 tool uses and 25.5 minutes of wall time (the third night: 2,145,771 tokens and
  45 minutes with fixed chunks of four). No slice hit the turn cap, and every slice returned DONE.

Totals from the verdict lines (241 signatures; programs in brackets): WISEOWL-CORRECT 114 (907) · GNUCOBOL-SIDE 54 (209)
· LATITUDE 41 (136) · NOT-COMPARABLE 14 (46) · UNINITIALIZED 11 (30) · MODEL-ERROR 5 (6) · WISEOWL-DEFECT 2 (5) · KNOWN
0 · FIXED-SINCE 0 · UNDECIDED 0. No program of the third night's fixes (PB3562 to PB3568, PB3570) or of PB3538
regressed.

## The findings (filed, held for the owner)

| Note | Title | Class | Witnesses |
|---|---|---|---|
| PB3664 | A fixed editing sign (`-`, `+`, CR, DB) reads the sending value's sign before truncation: `MOVE -0.001 TO PIC -9.99` prints `-0.00` where the stored value is zero | wrong answer (MAJOR, runtime/edit) | fz05181 (B2's 15.89 signature; its other three programs are TAN latitude) |
| PB3665 | STRING accepts `FUNCTION INTEGER(n)` as a sending operand and refuses `FUNCTION LENGTH(g)` as "a numeric literal"; one arm is wrong, and the message is false either way | adjudication (MINOR; the slice's verdict was rejects legal source, but §15.2 states no usage for integer functions, so SR1 may bar both) | fz01907 (C3) |
| PB3666 | MOVE of alphanumeric text to a USAGE BIT item stores the characters: DISPLAY shows `AB000`, B-NOT treats it as `00000` | analysis (D2's lead, no defect verdict) | fz00694 (D2) |

The operator narrowed PB3665 from the slice's verdict. The adjudicator read SR1's literal half, which is plainly
misapplied to a function-identifier (§8.4.3.2.1). SR1's identifier half ("described implicitly or explicitly as usage
display or national") meets §15.2, which gives numeric and integer functions no implicit usage. So whether the INTEGER arm
under-rejects or the LENGTH arm rejects legal source is a question for the spec, with GnuCOBOL's acceptance as the
latitude tiebreak if the spec leaves it open.

## Side observations (no finding filed)

- **A1, fz00167:** under native arithmetic, `2 ** 200` is formed on the 34-digit SDIDI (DOC-A.1-123), so the digits left
  after high-order truncation are 0, where GnuCOBOL's exact power leaves ...782792835301376. §8.8.1.3 leaves this to the
  implementor; the owner may want to see it under the rule-1 precedence.
- **B1:** COS(3.14159265) rounds to -1 in binary64, while the truncated exact value is -0.99999999 (native binary64
  latitude, the open owner question). MOVE HIGH-VALUE to a numeric-edited item leaves U+FFFF characters (undefined,
  §14.9.25.4 6) d) 1.).
- **C1, fz02732:** the first diagnostic, `COBOL0001: unexpected 'IF'`, points at a legal IF; the real error is the next
  one. This is the resync-noise class of open notes PB1321 and PB3248, with the noise BEFORE the real error, so it is a
  witness for those notes.
- **C2, fz01548:** the VALIDATE statement draws only warning 1580, while the facility's CLASS clause draws error 1708 (the
  10-23 record settled the 1708 refusal as correct).
- **C2 and C3:** COBOLNET0864's wording on `READ … KEY` under sequential access is open lead PB3569.
- **B4, fz00262:** an out-of-bounds subscript read with checking off reads 0 and prints nothing, a witness for PB2648.

## Recurring families (merged from the slices' tallies; each is a row of an earlier record)

- **Output differences:** argument violations returning DOC-A.1-90's 0 (NUMVAL formats, TEST-NUMVAL positions,
  DATE-TO-YYYYMMDD and DAY-TO-YYYYDDD ranges; B1, 104 programs); `0 ** 0` with no phrase (A1, 24); a zero divisor or
  other unchecked fatal exception ending the run unit (A1, N1); left-to-right exponentiation (A1, 3); overpunch letters
  (PB803; A1, 11); national items printed as UTF-16 bytes by GnuCOBOL (A1, B1, B3); last-digit intrinsic approximations
  (ANNUITY, COS, TAN and others; B1 to B4); EXCEPTION-STATUS inside ON SIZE ERROR (B1, 14); time of day and CURRENT-DATE
  (B1, 28); uninitialized items (30 programs).
- **Refusals, all correct:** national VALUE (0898, 85+), LINE SEQUENTIAL at 2014 (0900, 65+), unary sign spacing (2993,
  64), a VALUE that is not representable (1625, 60) or longer than its PICTURE (1740, 52), argument class (1627, 36+), an
  index-name as an arithmetic operand (1637, 35), USAGE NATIONAL with an X picture (0881, 32+), Table 16 MOVEs (0819, 23+),
  FORMATTED-* format arguments (1517, 23+), more than 31 digit positions (0801, 13), a conditional statement inside an
  imperative statement (2796, 13), RAISE of a non-exception-name (0711, 10), and the smaller families of the 10-22 and
  10-23 records. New and correct: CALL Format 1 with BY VALUE (0899, PB130), a free-form line over 255 positions (2653),
  an ordering relation on POINTER items (0869), STRING INTO an edited receiver (1651), and a paragraph outside a section in
  a sectioned procedure division (2797).
- **The GnuCOBOL side:** 2014 features GnuCOBOL 3.2 lacks (DISPLAY-OF 31, NATIONAL-OF 20, CONSTANT RECORD 19, ORDER TABLE
  16, DYNAMIC LENGTH 9, GROUP-USAGE 6, NUMVAL-C ANYCASE 7, subscript ALL 4, the alphabet-name class condition 4, ALIGNED 3,
  BOOLEAN class, object references, JUSTIFIED on a boolean item); functions it does not implement at run time
  (STANDARD-COMPARE 6, BOOLEAN-OF-INTEGER 3, CHAR-NATIONAL 3, EXCEPTION-LOCATION-N 3+); cases where it is stricter than
  the standard (MOVE SPACE to a numeric item, legal until 2023, 10 programs; a condition-name pair in EVALUATE; a PICTURE
  implied from VALUE); and cobc crashing on a program that CALLs itself (5).
- **The harness:** six NO_PROGRAM rows are model replies cut off at 6,000 tokens, and one RUNNER_ERROR row is PB3561's
  class.
