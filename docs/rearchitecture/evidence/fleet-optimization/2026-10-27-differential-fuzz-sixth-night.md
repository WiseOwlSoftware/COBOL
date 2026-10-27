# 2026-10-27 — The sixth overnight differential fuzz run, triaged by twelve Opus adjudicator slices

Experiment record (owner standing rule 2026-09-28: every fleet experiment goes here). Predecessors: the 2026-10-20 to
2026-10-26 records in this directory, the latest `2026-10-26-differential-fuzz-fifth-night.md`, whose method this night
repeats. Raw data: `E:\COBOL\conformance-suite\results\fuzz-20261026-225310\` (`run.json`, `results.jsonl`, `report.md`,
`programs/`). The adjudicators' reports and verdict files are session scratch (`fuzz6\adj\<slice>\` in session ef566665's
scratchpad) and are described inline. Nothing here changed a golden or the compiler.

**The owner's instruction** (2026-10-24, standing for every night): "run the fuzz analysis and report its findings. Do not
begin fixing any fuzz identified issues without my explicit approval". The one finding, PB3869, was filed `status: owner`; the owner approved its fix on 2026-10-27 at about 10:10 PDT ("Approve fixing PB3869; hand it to the operator").

**What gated this run.** The owner required the thirteen notes of the evening of 2026-10-26 to land before the run (PB3537,
PB3561, PB3839, PB906, PB3468, PB3569, PB2923, PB3514, PB3554, PB3635, PB3294, PB3697, PB3860); the last, PB3514, landed as
#687 at deae52c90, and the judge was built from that commit.

## The run

| | |
|---|---|
| Started | 2026-10-26 22:53 PDT (`run.json` `started` 05:53:10 UTC on 2026-10-27), `--hours 8 --jobs 4`, seed 20261027; ended 06:53 PDT, exit 0 |
| Generator | `qwen3.6:35b`, temperature 0.3, 3 variations, up to 3 repair rounds; 235 seeds of the prefixes `14.9`, `15`, `13.18`, `8.8` (23 declined) |
| Judges | WiseOwl `--std 2014` from `E:\tmp\fuzz-judge-20261027\cobol.exe` (main deae52c90); GnuCOBOL 3.2 `-std=cobol2014` under WSL |
| Programs | 6,646 judged; median model time 12 s a program |
| Usable (both compiled) | 4,183 (63 %), 2,104 of them after a repair round; 2,079 (31 %) on the first attempt |
| The model's failures | 2,463 (1,749 ended early on a repeated diagnostic) |

Classes (`report.md`): `BOTH_REJECT` 2,448 · `AGREE` 2,320 · `WE_REJECT` 1,100 · `OUTPUT_DIFFERS` 548 · `THEY_REJECT` 185 ·
`THEY_FAIL_RUN` 22 · `NO_PROGRAM` 10 · `BOTH_HANG` 5 · `EXIT_DIFFERS` 5 · `WE_FAIL_RUN` 2 · `WE_HANG` 1. The report's
survivors were 1,863 programs in 328 signatures; the slices decided 260 signatures covering 1,773 programs (the slicer drops
a `WE_REJECT` signature of one or two programs whose codes a larger signature already carries).

## Triage method (the fifth night's, unchanged)

- **No re-judge.** The judge was built from deae52c90, which was still origin/main at triage, so the recorded results
  stand. The output-difference rows were rescored from the recorded outputs (575 rows: 516 still differ, 32 agree once
  trailing spaces and NUL bytes are normalized, 21 `THEY_FAIL_RUN`, 6 `EXIT_DIFFERS`).
- **Slicing.** Twelve slices: A1 (§8.x output differences, 17 signatures), B1 to B4 (the other output differences, 88), C1
  to C4 (`WE_REJECT`, 95), D1 and D2 (`THEY_REJECT`, `THEY_FAIL_RUN`, 46) and N1 (hang, exit, run-failure and no-program,
  14).
- **Rolling pool.** One Workflow ran twelve read-only Opus `cobol-adjudicator` agents four at a time. The standing brief
  carried the fifth night's lesson (before calling a refusal wrong, look for the more specific rule and refute the
  compiler's own argument) and listed every note landed since, so a program of a landed note that still differed would be
  named a regression.
- **The orchestrator's re-run.** The one defect's repros, on the judge: `DIVIDE 1 BY 0 GIVING Q NOT ON SIZE ERROR ...`
  prints `Q=7.00 F=-` and exits 0; `ADD 1 TO X NOT ON SIZE ERROR ...` (PIC 9 VALUE 9) prints `X=9` beside the phraseless
  twin's `Y=0`. The citation (§14.7.5) passed `cite.py --check`.
- **Cost.** 1,964,246 subagent tokens, 962 tool uses and 27.4 minutes of wall time. Every slice returned DONE.

Totals (260 signatures; programs in brackets): WISEOWL-CORRECT 123 (1,202) · GNUCOBOL-SIDE 59 (296) · LATITUDE 42 (163) ·
NOT-COMPARABLE 17 (71) · UNINITIALIZED 11 (28) · MODEL-ERROR 7 (10) · WISEOWL-DEFECT 1 (3) · KNOWN, FIXED-SINCE and
UNDECIDED 0.

**No regression.** No program of a landed note, the thirteen pre-run notes and the fifth night's PB3829 to PB3832 included,
differed or was refused; B2 confirmed PB3666's alphanumeric-to-bit conversion holds in three programs.

## The finding (filed for the owner)

| Note | Title | Class | Witnesses |
|---|---|---|---|
| PB3869 | An arithmetic statement with only `NOT ON SIZE ERROR`, checking off, takes the SIZE ERROR phrase's disposition: a zero divisor continues with the receiver unchanged, and an oversized result keeps the old value, where §14.7.5 makes the NOT phrase "ignored" and the no-phrase rules (DOC-A.1-70: terminate, or store the low-order digits) apply | defect, wrong answer, silent (MAJOR, arithmetic) | fz00046 (B2); DIVIDE, ADD, COMPUTE and MULTIPLY reduced repros |

The adjudicator located the cause at `ArithmeticEmitter.EmitArith`, which takes the checked path whenever the phrase object
is non-null, and named the siblings to sweep (NOT ON EXCEPTION, NOT AT END, NOT INVALID KEY, NOT ON OVERFLOW). It also
found the register silent: PB1147 and PB441 quote the same sentence and line for other, landed defects.

## Side observations (no finding filed except as noted)

- **C3, fz06606:** after a true COBOL2269 the compiler emits a second 2269 and a false COBOL0901 ("'END-ACCEPT' is a
  reserved word"). Appended to open note PB3843 as a witness (a new code in its recovery cascade).
- **C3:** COBOL1694 on a POINTER operand also cites §13.18.60.3 SR10, the index-data-item rule (fz01875); wording only.
- **C2, fz01068:** COBOL0864's wording names sequential access, the wording landed PB3569 chose on purpose (docs/CONFORMANCE.md D-READ-KEY-ACCESS); the refusal is right. No finding.
- **B1:** `TEST-NUMVAL("")` and `TEST-NUMVAL-F("")` return 1 (a zero-length literal is legal, §8.3.3.1; rule c) gives
  LENGTH+1); GnuCOBOL reads "" as one space and returns 2. A new witness of the TEST-NUMVAL family, WiseOwl correct.
- **B1, fz00328:** a MOVE into the item's own REDEFINES is an overlap the standard leaves undefined (§14.6.10 1)).
- **A1:** `""` is a zero-length literal in concatenation and STRING too (§8.3.3.1); GnuCOBOL's one space is its side.
- **D1:** a new GnuCOBOL-side member: the table-format VALUE `VALUE IS 0 FROM (1) TO (10)` (§13.18.63.2), refused with
  "unexpected FROM" (2 programs). cobc again crashed on a program that CALLs itself (9 programs).
- **D2, GnuCOBOL 3.2 stricter than the standard, four members no earlier record names (one program each):** a separator
  comma in `BYTE-LENGTH(WS-NAT, PHYSICAL)` is refused although §5.2.8 lets a comma stand wherever a separator space does
  (probed under WSL: the same program without the comma compiles); the national-literal format `N"YYYYMMDD"` of
  FORMATTED-DATE is refused although §15.39.3 1) allows it (probed: the alphanumeric literal compiles); `PIC S9(3)V9(20)
  BINARY` (23 digits) is refused, by what reads as GnuCOBOL's 18-digit binary limit, where §13.18.40.3 14) allows 1
  through 31; and `FUNCTION CURRENT-DATE()` with an empty argument list is refused. The last two rest on the run's
  recorded cobc error only, with no reduced probe yet (PB3838's harvest reduces them). WiseOwl accepts all four, correctly.
- **N1:** every `NO_PROGRAM` row is a model reply cut off at 6,000 tokens (one by Ollama's repeat limit) whose saved
  program both compilers refuse; the one `WE_HANG` is the program's own PERFORM/GO TO loop.

## Recurring families

Unchanged from the fifth night's list, with the same verdicts: argument violations returning DOC-A.1-90's 0, TEST-NUMVAL
positions, `0 ** 0` with no phrase (A1, 30), unchecked fatal exceptions ending the run unit, overpunch letters (PB803),
national items printed as UTF-16 bytes by GnuCOBOL, last-digit intrinsic approximations, DOC-A.1-92's literal form,
EXCEPTION-STATUS with no TURN directive, time of day, uninitialized items, SYNCHRONIZED slack bytes; the refusals of the
earlier records (LINE SEQUENTIAL at 2014 108, a national VALUE 109, a VALUE not representable 93, unary sign spacing 73,
argument class 62, USAGE NATIONAL with an X picture 51, a VALUE longer than its PICTURE 49, FORMATTED-* format arguments 42,
an index-name as an operand 34, more than 31 digit positions 28) were all correct again; and the GnuCOBOL side's 2014
features it lacks and functions it does not implement at run time.
