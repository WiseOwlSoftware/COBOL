# 2026-10-22 — The second overnight differential fuzz run, triaged by delegated adjudicators

Experiment record (owner standing rule 2026-09-28: every fleet experiment goes here). Predecessors:
`2026-10-20-differential-fuzz.md` (the harness, kb/Work PB3463, and its yield work, PB3488) and
`2026-10-21-differential-fuzz-overnight.md` (the first night: PB3489, PB3490, PB3491, and the table of GnuCOBOL's known
holes). Raw data: `E:\COBOL\conformance-suite\results\fuzz-20261021-211424\` (`run.json`, `results.jsonl`, `report.md`,
`programs/`). The triage scripts and the adjudicators' verdict files are session scratch and are described inline.
Nothing here changed a golden; the four defects it found are kb/Work notes (PB3510, PB3511, PB3512, PB3517).

## The run

| | |
|---|---|
| Command (owner, 21:14 PDT 2026-10-21) | `differential_fuzz.py --model qwen3.6:35b --hours 8 --jobs 4 --seed 20261022 --clauses 14.9,14.6,14.7,8.8,8.4,8.5,13.18 --cobol E:\tmp\fuzz-judge-20261021\cobol.exe` from `E:\COBOL` |
| Generator | `qwen3.6:35b` on the RTX 5090, as on the first night |
| Judges | WiseOwl built from main at 20:38 on 2026-10-21 (BEFORE PB3489, PB3504 and PB3505 landed overnight), `--std 2014 --source-format free`; GnuCOBOL 3.2.0 `-std=cobol2014` under WSL |
| Seeds | 274 clauses of the seven listed prefixes, shuffled, 3 variations each, up to 3 repair rounds |
| Programs | 4,931 written and judged in 8 h; median model time 18 s a program |
| Usable (both compiled) | 2,592 (53 %), 1,646 of them after a repair round |
| The model's failures | 2,339 (`BOTH_REJECT` 2,326, `BOTH_HANG` 7, `NO_PROGRAM` 6) |

Classes: `BOTH_REJECT` 2,326 · `AGREE` 1,042 · `WE_REJECT` 707 · `OUTPUT_DIFFERS` 368 · `OUTPUT_LATITUDE` 290 ·
`THEY_REJECT` 154 · `THEY_FAIL_RUN` 19 · `BOTH_HANG` 7 · `NO_PROGRAM` 6 · `WE_HANG` 5 · `EXIT_DIFFERS` 5 · `WE_CRASH` 1 ·
`THEY_HANG` 1. Survivors: 1,260 programs in 314 signatures. PB3490's `BOTH_HANG` class and the `OUTPUT_LATITUDE` fold
(the display form, PB3467) did their job: 297 rows that the first night presented as leads were set aside by the report.

## Triage method

The standard decides (CLAUDE.md rule 1); GnuCOBOL is a net. Because the judge predated three overnight landings, every
lead was first re-run on a fresh build of main at fd407980f (a detached worktree, `E:\COBOL-wt\fuzz-triage-20261022`,
built at 09:25). The crash-class rows (hangs, the crash, the exit differences) were read by the session itself. Every
`OUTPUT_DIFFERS`, `EXIT_DIFFERS` and `THEY_FAIL_RUN` row (392) was re-judged mechanically on the fresh build against
GnuCOBOL's recorded output with the harness's normalization (per-line trailing blanks stripped, NUL read as space): 144
rows now agree (PB3505's decimal display form), 242 still differ, 6 differ in exit status only. On the owner's
instruction (10:00 PDT: "We should be using the appropriate models (Opus, Sonnet, etc.) for the work") the remaining
reading was delegated: three Opus `cobol-adjudicator` slices (the §8.x residue, 52 signatures; the §13/§14 residue, 48;
the refusal classes, 195), one verdict per signature from a fixed vocabulary with a `cite.py --check`ed rule, each
appended to a checkpoint file the moment it was decided; a Sonnet `cobol-clerk` filed the notes from the session's
structured findings. The session re-ran and re-cited every defect before filing it.

Cost of the delegation: each slice's first agent reached the 160-turn cap (two with no report written; slice C with
every `WE_REJECT` row decided), and a fresh agent finished each from the checkpoint in under half the tokens — the cost
law of the agent-fleet skill, observed again: 284 k / 274 k / 336 k tokens for the capped agents, 140 k / 141 k / 253 k
for their replacements, which decided the harder or larger remainders. The checkpoint files lost nothing. The subagent
harness refused each adjudicator's report file ("Subagents should return findings as text"), so the reports were
returned as text and saved by the session; the briefs of the next run should ask for the report in the return text
from the start.

## Hangs, crashes and early exits (12 rows, all examined by the session)

| Rows | Finding | Verdict |
|---|---|---|
| fz04522, fz00062, fz02549 (`WE_HANG`, compile) | Each refuses in under a second on main | PB3489 / PB3504 duplicates (the judge predated the fixes); no finding |
| fz01530 (`WE_CRASH`, "Stack overflow.") | A level-05 `OCCURS … DEPENDING ON` entry under a level-01 that carries a PICTURE, so the "elementary" item is both the ODO group and data-name-1. `ReferenceResolver.WrapIfOdoGroup` resolves the depending item, which is the group it is wrapping, without end. Reduced: any `DEPENDING ON` naming an ANCESTOR of the table crashes; naming an item inside or after the table is correctly refused (COBOLNET0853/0856/0857) | **WiseOwl defect — kb/Work PB3510** (crash on an illegal program; SR17/SR20's diagnostics exist and never fire) |
| fz00481 (`WE_HANG`, run) | `PERFORM CASE-ONE-PARA`, which does `GO TO EXIT-CASES`, the last paragraph; control runs off the end. WiseOwl returns to the PERFORM and continues (the later PERFORMs include a genuine infinite loop); GnuCOBOL ends the run. Reduced to a four-paragraph program: WiseOwl prints `P1 P3 AFTER PERFORM`, GnuCOBOL `P1 P3` | **WiseOwl defect — kb/Work PB3511** (§14.6.3 item 3: an implicit GOBACK; `DispatchEmitter`'s nested `__Dispatch` returns `__pc` at `__N`). Slice B found a second witness with no GO TO: `PERFORM P2 THRU P1` whose exit is never reached (fz04138) |
| fz03769 (`WE_HANG`, run) | `GO TO MAIN-PARA` from a performed paragraph re-performs everything forever; GnuCOBOL exit 11 | The model's loop (the first night's row); no finding |
| fz00510 ×4, fz00801 (`EXIT_DIFFERS`) | `STOP RUN` 1234 against 210; a NULL BASED pointer (exit 1 against 11) | The first night's two classes; no finding |
| fz04406 (`THEY_HANG`), 7 `BOTH_HANG` | Reference position 81 of an 80-position item (WiseOwl stops with EC-BOUND-REF-MOD, GnuCOBOL loops); seven loops of the model's own | Model errors; no finding |

## Output differences (368 rows; 242 after the re-judge on main)

| Slice | Signatures | LATITUDE | UNINITIALIZED | WISEOWL-CORRECT | GNUCOBOL-SIDE | NOT-COMPARABLE | MODEL-ERROR | WISEOWL-DEFECT |
|---|---|---|---|---|---|---|---|---|
| A (§8.x) | 52 (120 programs) | 20 | 14 | 6 | 8 | 1 | 1 | 2 signatures, one defect |
| B (§13, §14) | 48 (128 programs) | 15 | 19 | 8 | 3 | 1 | 1 | 1 (a PB3511 witness) |

The one new defect: **a `MOVE` of a signed numeric sender into a reference-modified alphanumeric receiver writes the
sender's raw stored form, sign included** (`2468` arrives as `246H`; a packed `-1234` as `-1234`; a SIGN SEPARATE
sender with its sign character; slice C's independent rediscovery adds a COMP sender). §14.9.25.4 GR6 a) says the
operational sign is not moved and §8.4.3.3.4 GR6 makes the slice an elementary receiver; `MoveClassifier.Kind` routes
every `RefModPlace` receiver to the `RefModSlice` arm before the sender's category is classified. **kb/Work PB3512.**

The rows that looked worst on their face were not defects, and the adjudicators' readings are worth keeping for the
next triage:
- `DIVIDE` results of `+0000000.00` against `-0081004.45` (10 rows): the `DIVIDE … INTO` receiver was never stored
  into; GnuCOBOL reads its space bytes as a number. Uninitialized.
- `EXTERNAL` items showing `0100` against `9900` (13 rows): an EXTERNAL item's VALUE takes effect only on INITIALIZE
  (§13.18.63.4 GR4 a)). Uninitialized, not a sign rule.
- FLOAT-LONG / FLOAT-EXTENDED into a fixed-point receiver differing in the last digit (15 rows): the standard leaves the
  conversion to the implementor; WiseOwl truncates the exact binary64 value (PB77 / PB623). Latitude.
- REDEFINES rows reading storage through another usage (12 rows): the overpunch letters are the implementor's
  (§13.18.52.4 GR4, PB803); arithmetic on non-numeric bytes is undefined (§14.6.13.2). Latitude.
- Dynamic-capacity table expansion contents (9 rows): GnuCOBOL 3.2 has no `OCCURS DYNAMIC`; what it compiled was not
  the standard's table. GnuCOBOL side.
- `COMPUTE R = 1.0E+50 + 1.0` storing 0.00 against 1.00: the native intermediate's precision is the implementor's
  (§8.8.1.3, DOC-A.1-123); a 34-digit decimal intermediate rounds 10^50 + 1 to 10^50. Latitude, recorded for the owner.
- `SQRT(1.234 * 1.234)` storing 1.233: an approximation (§15.84.4 rule 4), truncated at the receiver's scale.
  Observation, not a finding: a SQRT beside a decimal operand enters through the shortest round-tripping decimal
  (PB1641), so the same SQRT alone and inside a decimal expression can end in different last digits.
- The first night's classes recurred as predicted: `0 ** 0`, national items printed as UTF-16 bytes by GnuCOBOL, last-digit
  intrinsic approximations, time of day, STANDARD-COMPARE failing at run time in GnuCOBOL, divide by zero with no SIZE
  ERROR phrase (DOC-A.1-70), and an integer function's value shown in literal form (DOC-A.1-92).

## Refusals (707 WiseOwl, 154 GnuCOBOL) and GnuCOBOL's run failures (19)

Every `WE_REJECT` signature was read with its refused source line: **123 signatures, 118 WiseOwl-correct by the cited
rule, 3 latitude, 2 model errors, none a refusal of legal source.** The first night's families account for 443
programs (LINE SEQUENTIAL at 2014: 151; a VALUE not representable: 78; national PICTURE: 50; mixed argument classes: 47;
national VALUE: 42; unary sign spacing: 38; Table 16 MOVEs: 12; EXIT SECTION, index-name as operand, IS OMITTED: 8 each;
NOT GREATER OR EQUAL: 1). New families, all correct: `SET numeric-item TO integer` (27, §14.9.39.3 SR4), `EXIT.`
sharing its sentence with other statements (13, §14.9.14.3 SR1), `RAISE` of a word that is not an exception-name (11),
and smaller ones (COBOLNET1657, 2168 with 2576, 1505, 0901, 0863). The 16 `COBOL0001` rows on `SET x TO LENGTH 3` are
the model's: Format 16 is `SET [SIZE OF] identifier TO integer`.

`THEY_REJECT`: 36 signatures (111 programs) are 2014 features GnuCOBOL 3.2 lacks or other GnuCOBOL-side causes
(CONSTANT RECORD 14, DYNAMIC 15, BOOLEAN 11, ROUNDED MODE 9, GROUP-USAGE 7, EXCEPTION-OBJECT 6, DISPLAY-OF /
NATIONAL-OF 7, FLOAT-BINARY-32 3, ALIGNED 1; object references 9; thirteen cobc crashes; eight `cobc -x` refusals of
PROCEDURE DIVISION USING, a harness artifact; "invalid RETURNING field" on a Format 1 CALL 3). 9 signatures are WiseOwl
correct where GnuCOBOL is stricter than the standard (a PICTURE implied from a literal VALUE, §13.16.3 SR9: 14 programs;
MOVE SPACE to a numeric item, legal at 2014, removed only at 2023: 10). 5 are documented extensions (COMP-3, COMP-1, SET
TO ENTRY) or implementor code names. **One is a WiseOwl defect: `SIGNED` / `UNSIGNED` is accepted on every USAGE**
(`COMP UNSIGNED`, `DISPLAY SIGNED`, `PACKED-DECIMAL UNSIGNED` compile clean and the word is ignored) where the format of
§13.18.60.2 allows it only on the four fixed-width binary usages. **kb/Work PB3517** (under-rejection).

`THEY_FAIL_RUN`: 16 of 19 are GnuCOBOL's `attempt to use non-implemented function` (STANDARD-COMPARE, CHAR-NATIONAL);
WiseOwl's output was checked against the rules on each and is correct except fz04443, which is PB3512 again.

## Reading

- **Yield of the night: four defects** — a crash (PB3510), two wrong answers (PB3511, PB3512) and an under-rejection
  (PB3517) — from 4,931 programs; again the control-flow and data-shape rows, not the function catalog. Against the
  first night (three defects from 5,599 programs) the yield per program is similar and the noise is much lower: after
  PB3505 the display form no longer dominates the output rows, and PB3490's class removed the model's own loops.
- **The delegated triage worked, at a cost shape to fix.** Three Opus slices plus one Sonnet clerk read 295 signatures
  and filed four notes while the session spent its own turns on the crash-class rows, the dispatch and the four
  verifications. Every first agent hit the cap; the briefs asked for one program per signature but the agents probed
  more than they needed. Next time: a cap of about 25 signatures per agent, the report returned as text, and a line in
  the brief that a signature's first program is read and the rest are sampled only for a candidate defect.
- **What this does not show:** whether the 144 rows that now agree hide anything (they were not re-read: agreement with
  GnuCOBOL after PB3505 is the net's verdict, not the standard's); the single-row `WE_REJECT` signatures whose code
  appears in a larger signature (read by family, not individually); and GnuCOBOL's thirteen crashes, not pursued.

## Decision

Defects filed (PB3510, PB3511, PB3512, PB3517). On the owner's instruction (10:35 PDT: "The other session has hits its
quota for a while, yet we have plenty. Let's fix these compiler bugs in this session using the appropriate model") the
three crash / wrong-answer notes went to an Opus rolling wave from this session (wave 1147) the same morning; PB3517
goes to the fix lane. The next run's judge is rebuilt from main (the operator's note); the harness keeps its fold. The
detached worktree `E:\COBOL-wt\fuzz-triage-20261022` is removed when wave 1147 has landed.
