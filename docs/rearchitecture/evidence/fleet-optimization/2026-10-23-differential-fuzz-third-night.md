# 2026-10-23 — The third overnight differential fuzz run, triaged by twelve Opus adjudicator slices

Experiment record (owner standing rule 2026-09-28: every fleet experiment goes here). Predecessors:
`2026-10-20-differential-fuzz.md` (the harness, kb/Work PB3463, and its yield work, PB3488),
`2026-10-21-differential-fuzz-overnight.md` (the first night) and `2026-10-22-differential-fuzz-second-night.md` (the second
night: PB3510, PB3511, PB3512, PB3517). Raw data: `E:\COBOL\conformance-suite\results\fuzz-20261022-163846\` (`run.json`,
`results.jsonl`, `report.md`, `programs/`). The triage scripts and the adjudicators' reports and verdict files are session
scratch (`fuzz3\` in the session scratchpad) and are described inline. Nothing here changed a golden; the seven defects it found
are kb/Work notes PB3562 to PB3568, and three leads are PB3569 to PB3571.

## The run

| | |
|---|---|
| Started | 2026-10-22 16:38 PDT (`run.json` `started` 23:38:46 UTC), `--hours 8 --jobs 4`, seed 20261024 |
| Generator | `qwen3.6:35b` on the RTX 5090, as on the earlier nights |
| Seeds | 233 clauses of the prefixes `14.9`, `15`, `13.18`, `8.8` (25 declined seeds), 3 variations each, up to 3 repair rounds |
| Judges | WiseOwl `--std 2014` from `E:\tmp\fuzz-judge-20261022\cobol.exe`; GnuCOBOL 3.2.0 `-std=cobol2014` under WSL |
| Programs | 4,756 written; 4,753 judged; median model time 14 s a program |
| Usable (both compiled) | 3,012 (63 % of judged), 1,506 of them after a repair round; 1,506 (32 %) usable on the first attempt |
| The model's failures | 1,741 (1,239 ended early on a repeated diagnostic) |
| GnuCOBOL alone refused (`THEY_REJECT` leads) | 172 |

Classes (`report.md`): `BOTH_REJECT` 1,734 · `AGREE` 1,633 · `WE_REJECT` 804 · `OUTPUT_DIFFERS` 377 · `THEY_REJECT` 172 ·
`THEY_FAIL_RUN` 13 · `EXIT_DIFFERS` 9 · `NO_PROGRAM` 5 · `RUNNER_ERROR` 3 · `WE_FAIL_RUN` 2 · `BOTH_HANG` 2 · `WE_HANG` 1 ·
`THEY_HANG` 1.

**The pause.** The owner paused the run for a game from 21:01 to 22:05 PDT. The wrapper ended Ollama's runner to free the GPU,
and on the resume the harness's four in-flight requests failed with an Ollama HTTP 500 and were recorded as terminal
`RUNNER_ERROR` rows: fz02696, fz02698 and fz02699 (the three lost programs). kb/Work PB3561 (open) specifies the lossless-pause
fix: a PAUSE file the harness honors, one re-queue of a request that fails under a pause, and a report that counts paused time
and re-queued programs.

## Triage method

The standard decides (CLAUDE.md rule 1); GnuCOBOL is a net. Every lead was first re-run on a fresh judge: main at e7cf38ce4, a
detached worktree `E:\COBOL-wt\fuzz-triage-20261023`.

- **Re-judge (the locator).** The 399 `OUTPUT_DIFFERS` (377), `EXIT_DIFFERS` (9) and `THEY_FAIL_RUN` (13) rows were
  re-run on the fresh judge: every program compiled and NOW 392 are `OUTPUT_DIFFERS` and 7 `EXIT_DIFFERS`. Rescored with the
  harness's normalization: 365 `OUTPUT_DIFFERS`, 24 `AGREE`, 10 `EXIT_DIFFERS`. The residue is 375 rows in 93 signatures (the
  §8.x class A: 11 signatures, 50 rows; the other clauses B: 82 signatures, 325 rows).
- **The plan did not fit everything.** `WE_REJECT` has 125 signatures and 804 rows; the selection rule kept 70 signatures and 738
  rows (slices C1 to C3); 55 small signatures whose codes appear in larger ones were not sliced. `report.md` has no sections
  for `BOTH_HANG` (2 rows: fz01978, fz03900) or `NO_PROGRAM` (5 rows); the locator synthesized them from `results.jsonl` into D3
  and N1.
- **Adjudication.** Twelve Opus `cobol-adjudicator` slices of at most 25 signatures, the report returned as text (the lesson of
  the second night), run four at a time through one Workflow: A1 (§8.x output differences), B1 to B4 (the other output
  differences), C1 to C3 (`WE_REJECT`), D1 to D3 (`THEY_REJECT`, `THEY_FAIL_RUN`, `THEY_HANG`, `BOTH_HANG`) and N1 (crash, hang,
  exit and runner classes). One verdict per signature from a fixed vocabulary, each citation run through `cite.py --check`, each
  verdict appended to a checkpoint file the moment it was decided. The session (Mythos) re-ran every defect on the judge and
  re-checked its governing citations before filing.
- **Cost.** 2,145,771 subagent tokens, 960 tool uses, 45 minutes wall. No slice hit the turn cap; every slice returned DONE.

Totals from the verdict lines (231 signatures): WISEOWL-CORRECT 100 · GNUCOBOL-SIDE 45 · LATITUDE 45 · NOT-COMPARABLE 13 ·
UNINITIALIZED 9 · MODEL-ERROR 9 · WISEOWL-DEFECT 10 signatures, reducing to 7 defects · UNDECIDED 0.

## The seven defects

| Note | Title | Class | Witnesses |
|---|---|---|---|
| PB3562 | A boolean function sender in a group MOVE copies its '0'/'1' text carrier, not its usage-bit image | wrong answer (MAJOR, codegen/move) | fz01430, fz02128, fz04225 (B1 and D1: two signatures, one defect) |
| PB3563 | A MOVE into a level-66 RENAMES THROUGH alias converts the numeric leaf it spans | wrong answer (MAJOR) | fz03077, fz03775, fz04473, fz04474 |
| PB3564 | The implicit filler bits after a USAGE BIT item in an alphanumeric group have no storage | wrong answer (MAJOR) | fz03422 (fz01323 is GnuCOBOL's missing BIT support) |
| PB3565 | COMPUTE boolean-receiver = FUNCTION boolean-function is refused (COBOLNET1511) | rejects legal source (MAJOR) | fz00030, fz02828 |
| PB3566 | The numeric literal zero as the VALUE of a numeric-edited item is accepted at 2002 and 2014 | under-rejection (MINOR) | fz01391, fz00667, fz02776 (12 programs, three signatures) |
| PB3567 | A positive signed numeric literal as the VALUE of an unsigned item is accepted | under-rejection (MINOR) | fz04158 |
| PB3568 | VALIDATE's operands are never bound | under-rejection (MINOR) | fz02791 |

Two things in the table are worth keeping for the next triage. PB3562 was found twice, from the output side (B1) and from the
GnuCOBOL-failure side (D1, where GnuCOBOL cannot run BOOLEAN-OF-INTEGER), and the two reductions agree. PB3566 overturns a
landed determination, the "Complement, measured" paragraph of kb/Work PB921, which read the last sentence of §13.18.63.3 6) as
an all-edition exemption for the literal zero; the note says so and the implementer updates PB921's determination.

## The leads

- **PB3569** (C1's side observation): COBOLNET0864's wording on a `READ ... KEY` in the default sequential access names phrases
  the statement does not contain; whether ISO bars Format 2 under sequential access is left to the implementer.
- **PB3570** (B1 observation 1; `decision`, open): the PB1113 group-move determination (`MOVE -15 TO G` stores `-15`) is not in
  CONFORMANCE.md and rule 1's latitude precedence points at GnuCOBOL, which drops the sign. An owner question.
- **PB3571** (PB3538's round-6 review lead R6-1): a zero standard-decimal function result prints at its held places; the
  documentation row DOC-A.1-92 and the golden set do not say so. Lands with PB3538.

Recorded by the adjudicators, not filed: the duplicate row id DOC-A.1-92 in `docs/CONFORMANCE.md` (open note PB984 covers it);
C2's COBOLNET0901 false message on `CLASS IS BOOLEAN` in a declined VALIDATE clause (the refusal is right, COBOLNET1708 is the
code it should carry; site `CobolDeclined.g4:133`); C3's two wording notes (COBOL0307 for `ROUNDED` after `REMAINDER`, COBOLNET1970
for a FOOTING phrase out of order); N1's generic COBOL0001 message on fz00787 (`FUNCTION ... TRUE`); and B3's and B4's native
binary64 latitude notes (EXP overflow digits, 16-digit SQRT and LOG near 1) for the owner.

## Recurring families (merged from the slices' tallies; each is a row of the 10-21 or 10-22 records)

- **Argument violation gives DOC-A.1-90's value 0 (10-21):** the DATE-TO-YYYYMMDD / DAY-TO-YYYYDDD signatures in B1, B2, B3 and B4;
  the NUMVAL, NUMVAL-C, NUMVAL-F and TEST-NUMVAL(-F) argument formats in B1.
- **`0 ** 0` with no phrase (10-21):** A1, 25 programs; left-to-right exponentiation (10-21 `2 ** 3 ** 4`) and a negative base with
  a non-integer exponent (§8.8.1.2 6) c)).
- **An unchecked fatal exception ends the run unit (10-21 fz03337, DOC-A.1-70):** zero divisor, `0 ** 0` and a NULL BASED pointer
  (EC-DATA-PTR-NULL, WiseOwl exit 1, GnuCOBOL exit 11): A1, B1, B2, B3, N1.
- **National items printed as UTF-16 bytes by GnuCOBOL (10-21):** B1, B2, B3.
- **Last-digit approximations of intrinsics, PI and E digits (10-21):** B1 (ANNUITY x19), B2, B3, B4.
- **An integer function's value shown in literal form (10-22, DOC-A.1-92):** B1, B2, B3, B4.
- **Time of day, CURRENT-DATE, RANDOM (10-21):** B1, B2, B3, B4.
- **Uninitialized items (10-21):** A1, B1, B2, B3 (EXTERNAL items take their VALUE only on INITIALIZE).
- **Overpunch letters (10-22, PB803):** A1, B1, B2.
- **STOP RUN status above 255 (10-21):** B2, N1.
- **The WE_REJECT families (10-21 and 10-22):** LINE SEQUENTIAL at 2014 (0900), national VALUE (0898), a VALUE not representable
  (1625), unary sign spacing (2993), an alphanumeric VALUE longer than its PICTURE (1740), USAGE NATIONAL with an X picture (0881),
  mixed argument classes (1627), index-name as an operand, Table 16 MOVEs (0819), FORMATTED-* format arguments (1517), `SET
  numeric-item TO integer` (2326), EXIT SECTION (0827). New families, all correct refusals: a PICTURE of more than 31 digit
  positions (0801), VALUE under a REDEFINES (2406), a numeric FILE STATUS item (2455), a numeric RECORD KEY, UNSTRING mixing
  national and alphanumeric operands (2306), DELETE on a sequential file, DIVIDE REMAINDER ROUNDED, condition-name VALUE rules and
  LINAGE phrase order.

## The GnuCOBOL side (the D slices)

- **2014 features GnuCOBOL 3.2 lacks (10-21 and 10-22 rows):** DISPLAY-OF / NATIONAL-OF (45 programs), ORDER TABLE (18), CONSTANT
  RECORD (17), the ANYCASE and LOCALE keywords of NUMVAL-C / TEST-NUMVAL-C (11), GROUP-USAGE (7), DYNAMIC LENGTH (6), subscript ALL
  in a function argument (2); BOOLEAN, object references, OCCURS DYNAMIC and FLOAT-BINARY-32 in D2.
- **GnuCOBOL stricter than the standard:** a PICTURE implied from a VALUE (§13.16.3 9)), MOVE SPACE to a numeric item (legal at
  2014), an INSPECT size mismatch refused at compile time, a condition-name pair in EVALUATE, and reference modification of a
  no-argument function.
- **Functions not implemented at run time:** STANDARD-COMPARE, BOOLEAN-OF-INTEGER, CHAR-NATIONAL, EXCEPTION-LOCATION-N.
- **cobc crashes** on a program that CALLs itself with RETURNING; **`cobc -x` refuses** a PROCEDURE DIVISION USING (a harness
  artifact).
- **New GnuCOBOL holes** (for the record's table): libcob ends the run on an unhandled invalid-key status 21 or 23 where §9.1.14
  4) continues (D1 and B3, confirmed under WSL); a CALL of a missing program runs both the ON EXCEPTION and the NOT ON EXCEPTION
  imperative statements (B2); floating insertion after high-order truncation edits the untruncated value (B2, §13.18.40.5 6)).
- **Documented extensions:** COMP-1, COMP-2 and COMP-3, LATITUDE under §4.2.10 1).

## Reading

- **Yield of the night: seven defects from 4,756 programs**, against four from 4,931 the night before (PB3510, PB3511, PB3512,
  PB3517). Three wrong answers, one refusal of legal source and three under-rejections; the wrong answers sit in bit-valued data
  moved as groups and in RENAMES, shapes the first two nights did not reach. The prefixes changed (`14.9`, `15`, `13.18`, `8.8`).
- **The 25-signature cap held.** No agent reached the turn cap and every slice returned DONE; on 10-22 every first agent capped.
  Cost 2,145,771 subagent tokens for 231 signatures, about 9,300 a signature.
- **What this does not show:** the 55 small `WE_REJECT` signatures that were not sliced (read by family, not individually); the 24
  rows that now agree after the harness normalization; whether native binary64 latitude (the B3 and B4 notes) is what the owner
  wants for the native lane.

## Decision

The seven defects go to the fix lane. Owner rule (standing): fuzz fixes land before the next run, so the next run's judge is
rebuilt from main after they land. The leads wait: PB3569 for the fix lane, PB3570 for an owner answer, PB3571 for PB3538's
landing. The detached judge worktree `E:\COBOL-wt\fuzz-triage-20261023` is removed after the fixes land.
