# 2026-10-21 — The first overnight differential fuzz run, triaged against the standard

Experiment record (owner standing rule 2026-09-28: every fleet experiment goes here). Predecessor: `2026-10-20-differential-fuzz.md`
(the harness, kb/Work PB3463, and its yield work, PB3488). Raw data: `E:\COBOL-wt\fuzz-yield\conformance-suite\results\fuzz-20261020-224517\`
(`run.json`, `results.jsonl`, `report.md`, `programs/`); the triage scripts of this record are session scratch and are described
inline. Nothing here changed a golden; the three defects it found are kb/Work notes (PB3489, PB3490, PB3491) and one owner decision
gained evidence (PB3467).

## The run

| | |
|---|---|
| Command (owner, 22:45 PDT 2026-10-20) | `differential_fuzz.py --model qwen3.6:35b --hours 8 --jobs 4 --seed 20261021` from `E:\COBOL-wt\fuzz-yield` (PB3488's branch, since merged as PR #354) |
| Generator | `qwen3.6:35b` on the RTX 5090, winner of bake-off 4 (14 of 20 usable, median 10 s; PB3488) |
| Judges | WiseOwl built from main at 0421be4df (the `cobol.exe` of 2026-10-18 19:41), `--std 2014 --source-format free`; GnuCOBOL 3.2.0 `-std=cobol2014` under WSL |
| Seeds | 258 clauses of §8.8, §13.18, §14.9 and §15 (2023's additions removed, Annex E), shuffled, 3 variations each, up to 3 repair rounds |
| Programs | 5,599 written and judged in 8 h (about 12 a minute, four in flight); median model time 16 s a program |
| Usable (both compiled) | 3,348 (60 %), 1,635 of them after a repair round |
| The model's failures | 2,251 (`BOTH_REJECT` 2,249, `NO_PROGRAM` 2) |

Classes: `BOTH_REJECT` 2,249 · `OUTPUT_DIFFERS` 1,141 · `AGREE` 1,023 · `WE_REJECT` 950 · `THEY_REJECT` 176 · `THEY_FAIL_RUN` 41 ·
`WE_HANG` 8 · `EXIT_DIFFERS` 8 · `NO_PROGRAM` 2 · `WE_FAIL_RUN` 1. Survivors (everything but the model's failures and `AGREE`):
2,325 programs in 378 signatures.

## Triage method

The standard decides (CLAUDE.md rule 1); GnuCOBOL is a net. Every lead below was re-run on a fresh build of main (62cd0d954, the
morning's HEAD, built before any run) and read against `specs/ISO_COBOL.md`; every citation passed `cite.py --check`. The report's
378 signatures were ranked by count; the crash-class rows (hangs, crashes, early exits) were all examined; the output rows were
first folded by a value canonicalization and the residue examined by signature; the reject rows were examined by diagnostic
code down to the 5-program signatures.

## Hangs, crashes and early exits (18 rows, all examined)

| Rows | Finding | Verdict |
|---|---|---|
| fz02262 (`WE_HANG`, compile) | The compiler does not finish on a syntax error inside ≥ 3 nested function-identifier argument lists; the same error at depth 1–2 is reported in < 1 s | **WiseOwl defect — kb/Work PB3489** (compiler hang on a typo; crash class) |
| fz00122, fz01771, fz04123 (`WE_HANG`) | Both compilers hung (`they.hung` is true in results.jsonl): unsigned PERFORM VARYING counters (−1 stores 1, §14.9.25.4) and a loop that changes no bound. The classifier tests `we.hung` first and has no both-hung class | **Harness defect — kb/Work PB3490**; no compiler finding |
| fz02574, fz03157, fz03508, fz04092 (`WE_HANG`) | GnuCOBOL exited 11 (segfault); WiseOwl overflowed its stack on three (recursive PERFORM of an active paragraph whose exit is never reached, `GO TO MAIN-PARA` from inside performed paragraphs re-performing them forever) and looped on one (a `PIC S9(2)` counter past −99 with negative subscripts). §14.9.28.4 GR2 makes the nesting undefined; the GO TO shapes are infinite loops by the standard | No finding. Observation: a PERFORM whose exit is never reached holds a `__Dispatch` frame, so an infinite loop of that shape ends in a stack overflow after ~10 s where an address-slot implementation would loop forever; not a conforming program's harm |
| fz03337 (`WE_FAIL_RUN`) | `COMPUTE X = Y / 0` with no SIZE ERROR phrase ends the run unit: `EC-SIZE-ZERO-DIVIDE (fatal)` | WiseOwl as documented: §14.6.13.1.3 item 8 leaves the unchecked disposition to the implementor, DOC-A.1-70 / kb/Work PB1605 chose termination (GnuCOBOL continues with the receiver unchanged; the rejected reading is recorded there for the owner) |
| fz02747 ×4 (`EXIT_DIFFERS`) | A BASED item dereferenced through a NULL pointer: WiseOwl `EC-DATA-PTR-NULL (fatal)`, exit 1; GnuCOBOL exit 11 | No finding (both abnormal; WiseOwl names the condition) |
| fz00383 ×4 (`EXIT_DIFFERS`) | `STOP RUN` with status 1234: WiseOwl exits 1234, GnuCOBOL 210 (1234 mod 256) | No finding (host latitude) |

## Output differences (1,141 rows)

**615 rows are one implementor latitude.** Canonicalizing both outputs to sign + digits (GnuCOBOL's `+00012.50` and WiseOwl's
`000125{` both become `+0001250`) makes 615 of the 1,141 rows equal. The sign half is the settled decision PB803 (IBM / Micro
Focus overpunch); the decimal-point half is the open owner decision **PB3467**, which gained this count as evidence. Until it is
answered, more than half of a run's output rows are this one latitude.

**The 526 residual rows, by what they turned out to be** (every signature of 4 rows or more read; the first differing line of
each examined after the canonicalization):

| What | Rows (approx.) | Verdict, with the rule |
|---|---|---|
| An item displayed before anything is stored in it (no VALUE clause): WiseOwl zeros / spaces, GnuCOBOL NUL bytes, spaces or stale bytes | ~150 (every "`X:` vs `X:        `" and "`000` vs spaces" pair) | Undefined initial content; no finding. The suite normalizer strips WiseOwl's trailing spaces and keeps GnuCOBOL's NULs, which is why the rows looked one-sided |
| `FUNCTION EXCEPTION-STATUS` / `EXCEPTION-LOCATION` inside an `ON SIZE ERROR` phrase: WiseOwl spaces, GnuCOBOL `EC-SIZE-ZERO-DIVIDE` | 27 | **WiseOwl correct**: the explicit phrase takes precedence and "the EC-SIZE exception condition is not raised" (the standard's exception-processing overview, line 39791 of the transcription, with §14.6.13.1.1: the last exception status is set only when a condition is raised) |
| Intrinsic results that differ in the last digits: `ANNUITY(0.05, 1)` 1.05 vs 1.04999, `COS(PI)` −1 vs −0.9999999, `SIN(PI/2)` 1 vs 0.99999999 | ~40 | WiseOwl's are the exact values; GnuCOBOL truncates an intermediate. §15.4 lets an approximation vary; no finding |
| `FUNCTION E`, `FUNCTION PI` into 31-digit receivers under native arithmetic: WiseOwl's digits past the 17th are the binary64 expansion (`2.71828182845904509079…`), GnuCOBOL prints the decimal constant | 21 | Latitude: §15.27.3 / §15.73.3 rule 1, "an implementor-defined approximation". Under `ARITHMETIC IS STANDARD-DECIMAL` (rule 3, exact value required) WiseOwl prints `2.718281828459045235360287471352` and `3.141592653589793238462643383279` — verified. Precedence (GnuCOBOL) would favour the decimal constant in the native lane too; recorded here, not filed |
| `0 ** 0`: WiseOwl size error, GnuCOBOL 1 | ~12 | **WiseOwl correct**: §8.8.1.2 item 6 a), a zero base requires an exponent greater than zero, else EC-SIZE-EXPONENTIATION |
| `2 ** 3 ** 4`: WiseOwl 4096, GnuCOBOL 349412352 | 1 | **WiseOwl correct**: operations of the same level evaluate left to right (§8.8.1.2 item 3): (2³)⁴ |
| `NUMVAL("  1 23.4")`, `NUMVAL-C("$ -10.00")`, `TEST-NUMVAL-F` of a 35-digit significand | ~45 | **WiseOwl correct**: the argument formats of §15.67.3 / §15.68.3 / §15.69.3 rule 2 (31 digits under native arithmetic); an invalid argument's value is DOC-A.1-90's documented 0 |
| `DATE-TO-YYYYMMDD(0, 10)`, `DAY-TO-YYYYDDD(0, 0, 2000)`: WiseOwl 0, GnuCOBOL 20000000 / 2000000; `DATE-TO-YYYYMMDD(154557)` = 20154557 | ~15 | **WiseOwl correct**: §15.23.3 / §15.25.3 rule 1 require a POSITIVE integer (0 is an argument violation, DOC-A.1-90's 0); the NOTE says the function does not check that the argument is a date, so 154557 → 20154557 by the returned-value formula |
| `COMBINED-DATETIME(d, 86400)`: WiseOwl 0, GnuCOBOL a value | 9 | **WiseOwl correct**: §15.17 argument-2 is a standard numeric time, below 86400 |
| Comparison of a national item moved from alphanumeric against `N"EFGH"`; `(A AND NOT B) OR (NOT A AND B)`; UPPER-CASE / REVERSE of national items (GnuCOBOL prints UTF-16 bytes) | ~25 | **WiseOwl correct** in each; GnuCOBOL's national support is the difference |
| Time-of-day and CURRENT-DATE values | ~10 | Not comparable; no finding |
| `GO TO PARAGRAPH-B DEPENDING ON WS-INDEX` from a performed paragraph, target outside the range: WiseOwl prints nothing and exits 0 | 1 (fz01959) | **WiseOwl WRONG ANSWER — kb/Work PB3491** (five-variant reduction in the note) |
| Everything else (signatures of 1–3 rows not examined individually) | ~170 | Open leads; same classes as above by their first lines |

## WiseOwl refusals (950 rows) and GnuCOBOL refusals (176)

Every `WE_REJECT` signature down to 5 rows was read with its refused source line. All are WiseOwl following the standard where
GnuCOBOL's dialect is wider: `ORGANIZATION LINE SEQUENTIAL` at `--std 2014` (132, a 2023 phrase, COBOLNET0900); a VALUE literal not
representable in the PICTURE (85, §13.18.63.3 SR2); mixed argument classes in ORD-MAX / MAX / FORMATTED-DATE (79 + 38, §15.71.3 r3 and
§15.3); a national item with an alphanumeric VALUE (62, SR5) or PICTURE (37, §13.18.60.3 SR12); a unary `-` with no following space
(60, §8.7.1, settled yesterday); FORMATTED-DATE's format argument as a data item (29 + 19, §15.39.3 AR1 "shall be a … literal");
MOVE of a noninteger function value to an alphanumeric item (24, Table 16); `NOT GREATER OR EQUAL` (16, COBOL0001 — the format
of §8.8.4.2.1 has `[NOT]` on GREATER / LESS / EQUAL and not on GREATER THAN OR EQUAL TO, confirmed on the rendered figure notes);
an unterminated IF inside an inline PERFORM (16, COBOLNET2796, the landed determination PB351); `IS OMITTED` on a non-parameter
(11, §8.8.4.8.3 SR1); `EXIT SECTION` in a paragraph (9, §14.9.14.3 SR9); an index-name as an arithmetic operand (9, §13.18.38.3);
`DISPLAY UPON SYSOUT` with no SPECIAL-NAMES entry (5, §14.9.11.3 SR2); a reference-modified STRING receiver (5, §14.9.43.3 SR4).
The `THEY_REJECT` rows are 2014 features GnuCOBOL 3.2 lacks (CONSTANT RECORD, DYNAMIC LENGTH, DISPLAY-OF / NATIONAL-OF,
GROUP-USAGE, class conditions), as the harness header predicts. The 41 `THEY_FAIL_RUN` rows are GnuCOBOL runtime failures on
programs WiseOwl ran (STANDARD-COMPARE, BOOLEAN-OF-INTEGER, the EXCEPTION-*-N functions); not read further.

## The GnuCOBOL side: bug or choice (read in the 3.2 source, 2026-10-21 afternoon)

The owner asked whether the four most common "WiseOwl correct" rows are GnuCOBOL bugs or GnuCOBOL choices. Read in the
GnuCOBOL source at the release tag `gnucobol-3.2` of the mirror `github.com/OCamlPro/gnucobol` (the WSL `cobc` is 3.2.0), the
branch head `gnucobol-3.x` being identical at each site, plus one probe compiled and run under the WSL 3.2
(`gnucobol-side/pow0.cob`, output `gnucobol-side/pow0.gnucobol-3.2.out`). This is a record of the net's known holes, not work:
GnuCOBOL's defects are not this project's (CLAUDE.md rule 1: the differential is a regression net, never authority). A future
triage that meets one of these rows should recognize it from this table instead of re-deriving it.

| Row | GnuCOBOL mechanism (file, release 3.2) | Bug or choice |
|---|---|---|
| `0 ** 0` → 1 | `libcob/intrinsic.c` `cob_decimal_pow`: a comment `/* 0 ^ 0 */` and `cob_set_exception (COB_EC_SIZE_EXPONENTIATION)` for the zero-exponent, zero-base case — the runtime knows the rule. But `cobc/tree.c` `cb_build_binary_op` folds `literal ** literal` at compile time under the default `constant-folding: yes`, and its `case '^'` returns the literal `1` whenever the exponent is 0, with no check. Every one of the 23 fuzz programs that hit the row wrote the operands as literals or `ZERO`. | **Bug** (the compile-time path bypasses a rule the runtime implements) |
| `2 ** 3 ** 4` → 349412352 | `cobc/parser.y`: `exp_factor: exp_unary EXPONENTIATION exp_factor` is right-recursive where the `exp` and `exp_term` rules for `+ - * /` are left-recursive; the result is 2^81 = 2417851639229258349412352, whose low ten digits the program's `PIC S9(10)` kept and whose low nine its `PIC -(10)` display showed. NEWS and both ChangeLogs carry no mention of the decision. | **Choice**, undocumented (Fortran's convention; §8.8.1.2 item 3 says left to right) |
| `DATE-TO-YYYYMMDD(0, 10)` → 20000000 (and `DAY-TO-YYYYDDD`) | `libcob/intrinsic.c` `cob_intr_date_to_yyyymmdd`: the check is `year < 0 || year > 999999` where §15.23.3 rule 1 says POSITIVE, and `year` has already been divided by 10000, so the upper bound can never trigger either. The comment above it: "The unusual year checks are as specified in the standard". `cob_intr_day_to_yyyyddd` has the same two faults. | **Bug** |
| `EXCEPTION-STATUS` inside `ON SIZE ERROR` → `EC-SIZE-ZERO-DIVIDE` | `libcob/numeric.c` `cob_decimal_div` sets `COB_EC_SIZE_ZERO_DIVIDE` unconditionally; the comment directly above it is a `FIXME` saying the exception should be set only when the division is NOT inside an arithmetic statement with a SIZE ERROR phrase. | **Known bug**, acknowledged in the source |

**A fifth hole, found by the probe and not in the overnight report:** with VARIABLE operands (`WS-Z ** WS-Z`, both 0) GnuCOBOL 3.2
takes the `ON SIZE ERROR` branch and ALSO stores 1 into the receiving item (`VAR: SIZE ERROR, R1=+00001`; R1 held 77 before).
§14.7.5 requires the resultant identifier to be unchanged when the size error condition exists and the SIZE ERROR phrase is
specified (section 14.7.5 rule 1, checked: the values of all of the resultant data items remain unchanged from the values they had at the start of the execution of the arithmetic statement). The runtime sets the exception in `cob_decimal_pow` but still
returns the value 1, and `cob_decimal_get_field` stores anything that is not NaN. A fuzz program that prints the receiving item
after a handled `0 ** 0` will therefore show a one-sided row (WiseOwl leaves the item, GnuCOBOL writes 1); it is GnuCOBOL's, not
ours. The probe's second statement also shows `FUNCTION EXCEPTION-STATUS` reporting `EC-SIZE-EXPONENTIATION` after the literal
`0 ** 0` that the folding reduced to 1 (so nothing was raised there): a stale value from the first statement. GnuCOBOL does not
clear the last exception between statements that raise none.

Not pursued: reporting any of these upstream (the owner's call), and GnuCOBOL's behaviour at `gnucobol-3.x` head beyond the
sites read.

## Reading

- **Yield of the night: three defects (one wrong answer, one compiler hang, one harness classification) and one count that an
  open owner decision needed**, from 5,599 programs and about 45 minutes of one Claude session's triage (10:16 to 10:59 by the scratchpad and commit stamps). The two compiler
  defects are shapes no suite case had: a transfer out of a PERFORM range by the DEPENDING form, and an error three calls
  deep. That is the fuzz doing what the suite cannot: writing the programs nobody thought to write.
- **The noise is structural and removable.** 615 rows are one latitude (PB3467); ~150 are uninitialized items; 27 are a
  settled exception rule; the WE_REJECT family is WiseOwl being stricter than GnuCOBOL by the standard. A report that folds
  the decided latitude and marks "uninitialized item displayed" would leave a few hundred rows, most of them GnuCOBOL's.
  Both are harness work for PB3488's owner once PB3467 is answered.
- **The model's programs are the limit, not the GPU.** 40 % of programs failed both compilers; the repair rounds lifted the
  usable count by half. The seeds that produced real findings were control flow (§14.9.17, §14.9.28) and nested expressions,
  not the function catalog the model finds easiest to write.
- **What this does not show:** anything about the 170 single-row residual signatures, which are unread; whether a second
  night with a different seed finds different shapes (the same three defects would recur under any seed, since the shapes are
  common); and whether GnuCOBOL's deviations beyond the four read in its source (next section) are bugs or choices.

## Decision

Defects filed (PB3489, PB3490, PB3491) for the fix lane; PB3467's evidence added; no change to the harness in this record. The
next run waits for PB3467's answer and PB3490's fix so its report is smaller; the worktree `E:\COBOL-wt\fuzz-yield` that holds
the raw results stays until this record's raw folder is copied beside it (`2026-10-21-differential-fuzz-overnight/`) or the
owner says the results can go.
