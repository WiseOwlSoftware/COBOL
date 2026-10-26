# 2026-10-26 — The fifth overnight differential fuzz run, triaged by twelve Opus adjudicator slices

Experiment record (owner standing rule 2026-09-28: every fleet experiment goes here). Predecessors:
`2026-10-20-differential-fuzz.md` (the harness, kb/Work PB3463), `2026-10-21-differential-fuzz-overnight.md`,
`2026-10-22-differential-fuzz-second-night.md`, `2026-10-23-differential-fuzz-third-night.md` and
`2026-10-24-differential-fuzz-fourth-night.md`. Raw data: `E:\COBOL\conformance-suite\results\fuzz-20261025-220901\`
(`run.json`, `results.jsonl`, `report.md`, `programs/`). The adjudicators' reports and verdict files are session scratch
(`fuzz5\adj\<slice>\` in session f2318ea1's scratchpad) and are described inline. Nothing here changed a golden or the
compiler.

**The owner's instruction** (2026-10-24, restated for each night): "run the fuzz analysis and report its findings. Do not
begin fixing any fuzz identified issues without my explicit approval". So every finding is filed with `status: owner`
(PB3829 to PB3832), which no lane plans. The owner asked for the summary on 2026-10-26 about 10:50, and it was given.

## The run

| | |
|---|---|
| Started | 2026-10-25 22:09 PDT (`run.json` `started` 05:09:01 UTC on 2026-10-26), `--hours 8 --jobs 4`, seed 20261026 |
| Generator | `qwen3.6:35b`, temperature 0.3, 3 variations, up to 3 repair rounds |
| Seeds | 235 clauses of the prefixes `14.9`, `15`, `13.18`, `8.8` (23 declined seeds) |
| Judges | WiseOwl `--std 2014` from `E:\tmp\fuzz-judge-20261026\cobol.exe` (main 6ed90bd1c); GnuCOBOL 3.2 `-std=cobol2014` under WSL |
| Programs | 6,274 written and judged; median model time 13 s a program |
| Usable (both compiled) | 3,922 (63 % of judged), 2,021 of them after a repair round; 1,901 (30 %) usable on the first attempt |
| The model's failures | 2,352 (1,698 ended early on a repeated diagnostic) |

Classes (`report.md`): `BOTH_REJECT` 2,339 · `AGREE` 2,149 · `WE_REJECT` 1,032 · `OUTPUT_DIFFERS` 531 · `THEY_REJECT`
182 · `THEY_FAIL_RUN` 19 · `NO_PROGRAM` 12 · `EXIT_DIFFERS` 7 · `WE_FAIL_RUN` 2 · `BOTH_HANG` 1. The report's survivors
were 1,773 programs in 314 signatures, and the slices decided 258 signatures covering 1,705 programs.

## Triage method (the fourth night's, with one change)

- **No re-judge.** The fourth night re-ran its output-difference and refusal rows on a fresh judge built from main. This
  night the judge was built from main 6ed90bd1c, and `src/` was identical between that judge and main at triage, so the
  re-judge could not have changed a verdict and was skipped. Each adjudicator recompiled its first programs on the judge
  anyway to read every diagnostic.
- **Slicing.** Twelve slices: A1 (§8.x output differences, 14 signatures), B1 to B4 (the other output differences, 86),
  C1 to C4 (`WE_REJECT`, 94 signatures), D1 and D2 (`THEY_REJECT`, `THEY_FAIL_RUN`, `BOTH_HANG`, 48) and N1 (exit,
  run-failure and no-program, 16; its brief said 17, the input held 16).
- **Rolling pool.** One Workflow ran the twelve read-only Opus `cobol-adjudicator` agents four at a time from one queue.
  Each agent decided one verdict per signature, ran `cite.py --check` on every citation, appended each verdict to a
  checkpoint file as it decided it, and returned its report as text (the harness refused every report file, so the
  message is the report).
- **The four re-runs.** The orchestrator re-ran four repros on the judge: `acosmin` prints `A 3.1415` and `B 3.1416`;
  `r0901` draws COBOLNET0901; `r14` draws COBOLNET2072 and COBOL0001; `r2796` draws COBOLNET2796 at (11,23).
- **Cost.** 2,077,082 subagent tokens, 862 tool uses and 27.0 minutes of wall time. Every slice returned DONE.

Totals from the verdict lines (258 signatures; programs in brackets): WISEOWL-CORRECT 123 (1,150) · GNUCOBOL-SIDE 50
(243) · LATITUDE 45 (184) · NOT-COMPARABLE 19 (72) · UNINITIALIZED 12 (36) · MODEL-ERROR 4 (9) · KNOWN 1 (1) ·
WISEOWL-DEFECT 4 (10) · UNDECIDED 0.

## The findings (filed, held for the owner)

| Note | Title | Class | Witnesses |
|---|---|---|---|
| PB3829 | COMPUTE ROUNDED from ACOS, ASIN, ATAN, RANDOM or FRACTION-PART truncates through PB65's codomain clamp: `COMPUTE R ROUNDED = FUNCTION ACOS(-1)` into `PIC 9V9999` stores 3.1415, not 3.1416 | defect, wrong answer (MAJOR, runtime/intrinsics) | fz00612 (B4) |
| PB3830 | Nested arithmetic size-error phrase binding: `SUBTRACT ... ON SIZE ERROR ADD 1 TO D NOT ON SIZE ERROR ... END-SUBTRACT` draws COBOLNET2796 (the MULTIPLY sibling too) | adjudication, rejects-legal-source candidate (MINOR, grammar) | fz02050 (C2; five more programs of its signature are correct refusals) |
| PB3831 | `CLASS IS BOOLEAN` draws COBOLNET0901 where the declined VALIDATE facility's refusal is COBOLNET1708 | defect, diagnostic wording only (MINOR) | fz02486 (C2) |
| PB3832 | COBOLNET2072 claims an IF lacks its imperative-statement when the fault is `LENGTH OF` (only after an open `IF .. ELSE IF .. END-IF`) | defect, diagnostic wording only (MINOR) | fz02783 (C3) |

Two readings the owner should see before approving anything:

- **PB3829 would overturn part of a LANDED fix.** The adjudicator found §14.7.4.3 4) requires "the nearest value that
  can be represented in the resultant identifier", and §15.8.4 1) bounds only the returned value of ACOS. PB65's clamp
  (`FromDoubleBounded`; inventory row RV-15.75.4-1, with RV-15.10.4-1 and RV-15.11.4-1 on the same clamp) cuts the
  rounded value back to floor(bound x 10^scale), and the golden `pb65_codomain_clamp` asserts the clamped values in its
  ROUNDED legs, which landed PB647 added. GnuCOBOL 3.2 prints 3.1416, which agrees with the adjudicator but is no
  authority. The owner decides whether the clamp and the golden are re-derived.
- **PB3830 rests on one reading of §14.5.1.** The adjudicator held that NOT ON SIZE ERROR belongs to SUBTRACT's format
  and ends imperative-statement-1, so the program is legal. WiseOwl's own diagnostic says an ADD written without END-ADD
  "is a conditional statement", and ADD's own format also has NOT ON SIZE ERROR, so which statement owns the phrase is
  the question. GnuCOBOL binds the phrase inward and prints `D=1 F=N`, so it settles nothing. Both readings are in the
  note.

## Side observations (no finding filed except as noted)

- **C1, fz02341:** `WRITE ... INVALID KEY SET WS-ERR TO "INVALID"` reports COBOL0307 at the INVALID keyword where the
  illegal statement is the SET. Appended as a dated paragraph to open note PB906 (a witness; PB765 also fits).
- **A1, a lead (not filed):** under native arithmetic an integer base raised to a power that does not fit Int128 takes
  binary64 `Math.Pow` (`PowNativeIntDec`, CobolIntrinsics.RealArgs.cs), so `B ** 1100` with `B PIC S9 VALUE 2` ends the
  run unit with EC-SIZE-OVERFLOW ("infinite floating-point operand exceeds the decimal128 range") although 2^1100 is
  inside decimal128, while the same value held in a fractional base takes PB2617's exact lane and continues. Four
  programs (fz00344, fz01047, fz04573, fz02380). Native arithmetic is the implementor's choice (§8.8.1.3), so the slice
  recorded LATITUDE; the two lanes disagree and the message is wrong, which the owner may want filed.
- **D2, fz02377:** PI moved into an `S9(3)V9(28)` item prints the exact binary64 digits, 3.1415926535897931159979634685,
  and PI*2 prints the shortest round-trip value. Latitude under §15.73.3 1) (DOC-A.1-92, PB647, PB1641), and a visible
  quality point: 13 digits of binary noise in a 28-place receiver.
- **D2, fz05091:** a literal subscript 3 on an OCCURS 2 table, read with checking off, returns 0. DOC-A.1-70 covers size
  truncation, not-finite values and zero divide; the slice's grep found no text for an EC-BOUND-SUBSCRIPT read. PB2504
  (open, item 70, the RAISE path) is close and does not cover it.
- **C4:** `IF A NOT <> B` draws "unexpected 'NOT'" and then COBOLNET2072's false missing-statement claim; it is a second
  witness for PB3832 and is recorded there. Only the first such line is reported (the resync-noise class of PB1321 and
  PB3248).
- **B3, fz05505:** at `--std 2014`, `PIC +ZZ,ZZ9.99 VALUE "+00,001,234.56"` (14 characters on a 10-position item) is
  accepted. COBOLNET1570 rejects it only at 2023, by design (the comment in DiagnosticCatalog.cs).
- **B1:** fz01631 and fz02336 de-edit a ZZ9 item whose VALUE left content that no editing can produce, which §14.6.13.2 4)
  makes undefined; WiseOwl reads position by position and GnuCOBOL takes the digits. UNINITIALIZED, no finding.
- **B2:** CHAR(255) and HIGH-VALUE reach the console as different bytes in the two compilers (latitude, §14.9.11.4 1)); the
  fourth night had covered HIGH-VALUE only inside a numeric-edited item. fz05202: SECONDS-PAST-MIDNIGHT keeps the
  fraction of a second where GnuCOBOL returns whole seconds (time of day, not comparable).
- **C3:** a 1511 message prints `FUNCTIONINTEGER-OF-BOOLEAN(1)` with the space dropped, which is open note PB2838.
- **N1:** every one of the 12 `NO_PROGRAM` rows is a model reply cut off at 6,000 tokens after both compilers refused the
  saved program, and the saved programs are illegal source (`FUNCTION INDEX`, `TRIM(x, BOTH)`, `INTEGER-OF-BOOLEAN(TRUE)`,
  `COMBINED-DATETIME` with one argument).
- **B4:** the other ten signatures had no defect: seven GnuCOBOL-side (functions it does not implement at run time),
  two latitude (a NUMVAL-F argument violation; SIN(PI/2) in binary64) and one uninitialized.

## Recurring families (merged from the slices' tallies; each is a row of an earlier record)

- **Output differences:** argument violations returning DOC-A.1-90's 0 (DATE-TO-YYYYMMDD, DAY-TO-YYYYDDD and the NUMVAL
  family; B1 79 programs, B2 about 21, B3, A1); TEST-NUMVAL positions (B1, 32); `0 ** 0` with no phrase (A1, 32); an
  unchecked fatal exception ending the run unit (a zero divisor, a NULL BASED pointer, EC-BOUND-REF-MOD; A1 7, B2 14,
  B3, N1 9); left-to-right exponentiation (A1, 2); overpunch letters (PB803; A1 7, B1 13, B2 6); national items printed
  as UTF-16 bytes by GnuCOBOL (B1 30, B2 12, B3); last-digit intrinsic approximations (ANNUITY, PI, SIN, COS, SQRT, TAN,
  EXP; B1 to B4); an integer function shown in literal form (DOC-A.1-92; B1, B2); EXCEPTION-STATUS with no TURN
  directive (B1, 15); time of day and CURRENT-DATE (B1 49, B2 11); uninitialized items (A1 4, B1 20, B2 7, B3 5);
  SYNCHRONIZED slack bytes (B1, 8); non-numeric content in a numeric item (B2, 10).
- **Refusals, all correct:** LINE SEQUENTIAL at 2014 (0900, 111+), a national VALUE (0898, 95+), a VALUE not representable
  (1625, 90), unary sign spacing (2993, 70), argument class (1627, 60+), a VALUE longer than its PICTURE (1740, 55+), an
  index-name as an operand (1637, 50), USAGE NATIONAL with an X picture (0881, 38+), FORMATTED-* format arguments (1517,
  37+), Table 16 MOVEs (0819, 24+), more than 31 digit positions (0801, 21), RAISE of a non-exception-name (0711, 12),
  and the smaller families of the earlier records. New and correct this night: `SET index-data-item TO 1` (2326, SR3), a
  USAGE INDEX item in DISPLAY or ADD (1694, 1675), two EXTERNAL entries with one externalized name (2159), an unspaced
  comma in a subscript list (0895 with 2097), ALIGNED on a `USAGE BIT` group (1942), a name that is both a data-name and
  an index-name (2692), PIC U (0808), `OPEN EXTEND ... WITH NO REWIND` (1803), a subscript index from another table
  (1961), `KEY IS` on an item that is not subordinate (2353), B-NOT on a condition-name (1639), `PERFORM index-name
  TIMES` (1646), CANCEL of a numeric item (1696), REDEFINES of a POINTER (1697), STRING with an ALL figurative constant
  (1757), `SET numeric UP BY` (2112), a `Z"..."` literal (2633), a LINKAGE record without USING (2746), and a GO TO that
  is not last in its sequence (2967).
- **The GnuCOBOL side:** 2014 features GnuCOBOL 3.2 lacks (NATIONAL-OF 26, DISPLAY-OF 29, ORDER TABLE and STANDARD-COMPARE
  24, CONSTANT RECORD 15, GROUP-USAGE 10, DYNAMIC LENGTH 5, NUMVAL-C ANYCASE 8, object references 6, ALIGNED 2, subscript
  ALL 2, the NEAREST-TO-ZERO class condition 2); functions it compiles and then does not implement at run time
  (BOOLEAN-OF-INTEGER 8, EXCEPTION-FILE-N 3, EXCEPTION-LOCATION-N 3, CHAR-NATIONAL 1, INTEGER-OF-BOOLEAN 1, STANDARD-COMPARE
  2; INTEGER-OF-BOOLEAN is a new member, confirmed under WSL); cases where it is stricter than the standard (a PICTURE
  implied from VALUE, an INSPECT size mismatch, a condition-name pair in EVALUATE, a compile-time subscript bound); cobc
  crashing on a program that CALLs itself (7, a segfault at fz00194) and an internal compiler error on BYTE-LENGTH
  (fz00381); the harness artifact that `cobc -x` refuses PROCEDURE DIVISION USING (14); "invalid RETURNING field" on a
  Format 1 CALL (2); the `-std=cobol2014` refusal of COMP-1 and COMP-3, which are WiseOwl's documented extensions (13);
  and GnuCOBOL's floating-insertion hole on `-B-B-B9` and its missing floating-point edited pictures (B3). No program of
  the fourth night's fixes (PB3664 to PB3666) regressed.
- **The harness:** twelve NO_PROGRAM rows are model replies cut off at 6,000 tokens, and the run had no `RUNNER_ERROR` or
  `WE_HANG` row.
