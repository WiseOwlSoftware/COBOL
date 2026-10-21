# 2026-10-20 — Differential fuzzing with a local model (kb/Work PB3463)

Experiment record (owner standing rule 2026-09-28: every fleet experiment goes here). The tool is
`scripts/differential_fuzz.py`; its runs write under `conformance-suite/results/` (git-ignored) and nothing here changed the
compiler, a golden, the suite or a `kb/Work/` verdict.

**The question (owner, 2026-10-20 10:40 PDT).** How could the RTX 5090 run a local model overnight on some of the project's
work, a focused 6-to-10-hour task; are file edits a fit? **The evaluation's answer:** only where a machine judges the model's
output. The three earlier records (the 2026-10-03 spike, PB2886's one-shot, the aider run of DEVLOG 714) each produced confident
output an expert had to refute, and an unsupervised compiler edit is that class of task. Differential fuzzing needs no judgment
from the model: it writes plausible COBOL from a spec clause, WiseOwl and GnuCOBOL 3.2 both compile and run it, and only a
disagreement survives. The owner: "Build the harness, but we'll try a small test run today."

**Hardware and software.** RTX 5090 (32 GB), 128 GB RAM, Ollama 0.35.1, GnuCOBOL 3.2.0 under WSL Ubuntu, WiseOwl built from
main at 5bec30cd2. Generator in these runs: `qwen3-coder:30b` (a mixture of experts, about 3B active parameters), temperature
0.6, 16k context, 6000 output tokens. Both compilers pinned to the 2014 edition, free reference format, through the
conformance suite's adapters (`conformance-suite/adapters/*.json`, run by the harness's own `Run.operation`); the GnuCOBOL steps
cross into WSL through the adapter's hook interface.

## The smoke runs (what broke, and what each fix was)

| Run | Programs | GnuCOBOL accepted | Finding |
|---|---|---|---|
| 1 (2 programs, MOVE) | 2 | 0 | The reply's fence read "```cob" followed by CJK characters, which the extractor did not accept; the seed was the bare "General formats" sub-clause, not the MOVE statement with its rules. Fixed: any fence info string; structural sub-clauses (General, General formats, Format n, Syntax rules, General rules, Arguments, Returned values) fold into their parent seed (394 seeds under the default prefixes; MOVE is one). |
| 2 (4 programs, 2 in flight) | 4 | 0 | Every reply was cut off at the 3000-token output cap: the model wrote fixed-form indentation and long filler data lists, the fence never closed, and the extractor took the whole reply (GnuCOBOL: "invalid symbol '`'"). Fixed: 6000 tokens; Ollama's `done_reason` "length" is detected and sent back as a repair request for a shorter program; the prompt forbids indentation and filler items; an unterminated fence still yields its program. |
| 3 (4 programs, 2 in flight) | **4 (3 after one repair round)** | — | AGREE 1, OUTPUT_DIFFERS 3 in two signatures (below). Median model time 24 s per program with two in flight. |

The repair round (new in run 3): a program GnuCOBOL refuses goes back to the model once with the first 30 diagnostic lines and
the instruction to fix it in standard COBOL 2014 without dropping the clause under test. In run 3 it turned three refusals into
compiled programs; the refusals were of the PB2886 kind (a MOVE CORRESPONDING on elementary items, a numeric literal in the
VALUE of a numeric-edited item).

## What run 3 surfaced (NOT adjudicated here; the morning triage checks each against the spec)

- `OUTPUT_DIFFERS:14.9.2` × 2 (ADD): DISPLAY of a PIC 9(5)V99 item after ADD prints `0012345` on WiseOwl and `00123.45` on
  GnuCOBOL. The question is DISPLAY's rendering of an implied decimal point, §14.9.11, not ADD; GnuCOBOL's rendering may be its
  own latitude. Likely already adjudicated; search the register before filing.
- `OUTPUT_DIFFERS:14.9.25` × 1 (MOVE): a boolean item after MOVE ZERO (`0` vs `00`), HIGH-VALUE into an alphanumeric item (the
  display of U+FFFF vs 0xFF bytes: PB2919's storage-byte law), LOW-VALUE into a national item, and the display of a group with
  national subordinates. GnuCOBOL itself warns "handling of USAGE NATIONAL is unfinished" on this program, so its side is weak
  evidence there.

## Reading of the results so far

- The mechanics hold: both compilers run through the suite's adapters, a kill loses nothing (`results.jsonl` is appended per
  program), the report groups survivors by signature, the model is unloaded at the end.
- The model's failure mode is the one PB2886 recorded, and it is now filtered mechanically (GnuCOBOL's refusal) and mostly
  repaired (one round). Its yield after repair was 4 of 4 in run 3; the 40-program test run below measures it properly.
- Output-token truncation, not COBOL knowledge, was the dominant loss in the first runs: a finding about prompting a local
  model, recorded so the next tool does not rediscover it.

## The 40-program test run (12:44–12:59 PDT; `--count 40 --jobs 3`, default seed prefixes)

The first attempt died in its third program on a harness bug (a repair round archived a source file the first reply never
produced); fixed in the tool, with the rule that any harness exception inside one program is a RUNNER_ERROR row, never the
end of the run. The second attempt ran to completion. Raw: `2026-10-20-differential-fuzz/test40-report.md`,
`test40-results.jsonl`, and the twelve survivors' sources under `survivors/`.

| Measure | Value |
|---|---|
| Programs | 40 in 14 min 23 s, three in flight (about 22 s of wall clock each; an 8-hour run at this pace writes about 1,300) |
| GnuCOBOL accepted | 16 of 40 (40 %); 13 of the 16 only after the one repair round; 37 of 40 needed a repair attempt |
| Median model seconds per program | 72 (both rounds counted) |
| Classes | GNU_REJECTS 24 · WE_REJECT 9 · AGREE 4 · OUTPUT_DIFFERS 3 · RUNNER_ERROR 0 |
| Survivors | 12 programs in 5 signatures |

What GnuCOBOL refused, mostly: a negative VALUE in an unsigned PICTURE ("data item not signed", the commonest), a group item
with a PICTURE, an undefined name, a PROCEDURE DIVISION header it did not find (four programs). These are the model's COBOL
slips; one repair round fixed about a third of the refusals. A second round (`--repairs 2`) would raise the yield at the cost
of model time, which is free overnight.

**The five signatures (leads for the triage, each to be checked against the standard; none adjudicated here):**

| Signature | × | What the row shows |
|---|---|---|
| `WE_REJECT:COBOLNET2993` | 6 | WiseOwl refuses a unary `-` not followed by a separator space (`** -1`), citing §8.7.1; GnuCOBOL reads `-1` as a signed literal. The clause's own rule decides whether the sign there is a unary operator or part of the literal. |
| `OUTPUT_DIFFERS:8.8.1.5.4` | 2 | DISPLAY of a signed numeric item after `0 ** n`: WiseOwl prints the storage form (`000000000000{`, a trailing overpunched sign), GnuCOBOL `+0000000.000000`; WiseOwl's run also ended before `0 ** -1` (a size condition, EC-SIZE-EXPONENTIATION, where GnuCOBOL printed zero). Two questions: DISPLAY's rendering of a signed item (§14.9.11), and the exponentiation size condition (§8.8.1.5.4). |
| `WE_REJECT:COBOLNET1625` | 2 | WiseOwl refuses the literal `-0.000001` in a VALUE clause as not representable in the PICTURE; GnuCOBOL accepts (truncating?). §13.18.63 VALUE rules decide. |
| `OUTPUT_DIFFERS:8.8.1.4.4` | 1 | DISPLAY of a PIC 9(10)V9(10) item: WiseOwl prints the twenty digits, GnuCOBOL inserts a decimal point. DISPLAY's treatment of an implied decimal point (§14.9.11); likely already adjudicated, search the register first. |
| `WE_REJECT:COBOLNET0801` | 1 | PICTURE 9(34): WiseOwl refuses (ISO §13.18.40.3 syntax rule 14 limits a numeric item to 31 digit positions), GnuCOBOL accepts up to 38 as its extension. The refusal is ours and correct; the row is a GnuCOBOL extension, the shape the differential's header warns about. |

Reading: five signatures from 16 compiled programs is a high lead density, and it will fall as the obvious latitude
(DISPLAY of signs and implied decimal points) is adjudicated once and recognised in later reports. The seed order matters:
the forty programs came from §8.8 (arithmetic) only, because the seed list is walked in clause order with three variations
each; after this run the seed list is shuffled by default (deterministically, `--seed`; `--in-order` restores the spec's
walk) so a run of any length samples every area.

## The five signatures checked against the standard (finisher, wave 1134, 2026-10-20)

Each lead re-run on a build of the branch (`cobol.exe --std 2014 --run`) and read against `specs/ISO_COBOL.md`; every
citation passed `cite.py --check`. Result: **no compiler defect among the 12 survivors**; four signatures are WiseOwl
following the standard where GnuCOBOL's default dialect is wider, one is an implementor-latitude difference.

| Signature | Verdict | Why |
|---|---|---|
| `WE_REJECT:COBOLNET2993` × 6 | WiseOwl correct | All six refused lines are `COMPUTE X = -IDENT …` (fz00003 line 71, fz00005 line 18, fz00016 line 21, fz00027 line 27, fz00028 line 34, fz00029 line 24). §8.7.1: the arithmetic operators "shall be preceded by a space and followed by a space except that no space is required between a left parenthesis and a unary operator or between a unary operator and a left parenthesis". `-A` has no space after the unary `-`, and an identifier is not a literal, so §8.3.3.3.2's literal sign cannot absorb it. GnuCOBOL's acceptance is its extension (kb/Work PB1430, PB2017 are the landed design). |
| `OUTPUT_DIFFERS:8.8.1.5.4` × 2, run end | WiseOwl correct | The WiseOwl run stops at `0 ** -1` with `EC-SIZE-EXPONENTIATION (fatal)`. §8.8.1.2 6) a): a zero base requires an exponent "greater than zero", otherwise the exception condition is set to exist; §14.6.13.1.6 lists EC-SIZE-EXPONENTIATION as **Fatal**; no SIZE ERROR phrase was written (fz00030, fz00032). GnuCOBOL continues with zero. The program's later `0 ** 0` is the same condition (§8.8.1.5.4 GR 4). |
| `OUTPUT_DIFFERS:8.8.1.5.4` × 2, signed display; `OUTPUT_DIFFERS:8.8.1.4.4` × 1 | Latitude, a lead for the owner | DISPLAY of a numeric DISPLAY-usage item: §14.9.11.4 GR 1, "Any conversion of data required between … the data item … and the device is defined by the implementor". WiseOwl prints the PICTURE's digits with the IBM / Micro Focus operational sign overpunch and no decimal point (`000125{`, owner decision PB803 for the sign, DOC-A.1-177 and DOC-A.1-56); GnuCOBOL's `pretty-display` prints `+00012.50`. The sign has an owner decision; the decimal point alone has no recorded one, and CLAUDE.md rule 1 puts GnuCOBOL ahead of IBM/Micro Focus where nothing else decides. Reported as a lead, not changed: the change would alter the output of every program that displays a scaled item. |
| `WE_REJECT:COBOLNET1625` × 2 | WiseOwl correct | `PIC S9(5)V9(5) VALUE -0.000001` (fz00020 line 24: six decimals into five) and `PIC S9(4)V99 VALUE -0.333` (fz00031 line 26: three into two). §13.18.63.3 SR 2: the values "shall be representable exactly in the subject of the entry, without truncation of leading or trailing nonzero digits". GnuCOBOL truncates. |
| `WE_REJECT:COBOLNET0801` × 1 | WiseOwl correct | `PIC 9(34)` (fz00026 line 9). §13.18.40.3 SR 14: "the number of digit positions … shall range from 1 through 31". The first version of this table cited §8.3.3.3.2, which is the literal rule; the compiler's own message and catalog entry carried the same inherited clause and now cite §13.18.40.3 SR14 for items and §8.3.3.3.2 for literals. |

## The yield changes (PB3488) — the evening of 2026-10-20, four bake-offs and a recount

Raw, in `2026-10-20-differential-fuzz/`: `bakeoff1-bake-off.md`, `bakeoff2a-qwen3.6-prompt-v1-report.md`, `bakeoff3-*`,
`bakeoff4-*` (each bake-off's scoreboard and every model's report). Every bake-off: 20 programs per model, seed 20261020,
two in flight, the WiseOwl build of main at 0421be4df, GnuCOBOL 3.2 under WSL.

**Bake-off 1 (20:36–21:2x PDT; the harness as landed in PB3463: one correction round, temperature 0.6, 394 seeds).** The
owner's choice of models after the evening's web research; the two lowest were removed afterwards.

| Model | GnuCOBOL accepted | Median model s | Lead rows |
|---|---|---|---|
| qwen3.6:35b | 10/20 (50 %) | 7 | 9 |
| qwen3-coder:30b | 8/20 (40 %) | 39 | 6 |
| laguna-xs-2.1 | 8/20 (40 %) | 20 | 6 |
| qwen3.8:27b | 4/20 (20 %) | 27 | 2 |
| nemotron-3.5-bolt | 3/20 (15 %) | 22 | 3 |

**The recount (21:5x PDT).** The owner asked whether the models' incorrect programs could be improved. The first reading of
GnuCOBOL's messages said two fifths of the refusals were constructs GnuCOBOL 3.2 lacks; the measurement said otherwise:
every refused program compiled by WiseOwl (`--std 2014 --source-format free`; raw: `bakeoff1-recount.txt`) — 67 refused
rows, 65 with a program, 2 with none; **WiseOwl compiles 1 of the 65.** The 64 others: 37 parse errors (COBOL0001 ×19,
COBOL0307 ×18), 6 COBOLNET0898 (a non-national VALUE on a national item), 4 COBOLNET1516 (HIGHEST-ALGEBRAIC on a literal;
§15.43.3 AR1), 3 each of COBOLNET2993, COBOLNET0801 and COBOLNET1941, 8 single codes. FUNCTION TRANSLATE, NATIONAL, SUBSTRING and YEAR are refused as not intrinsic functions of
ISO/IEC 1989 (COBOLNET1501, §15.6); COMP-3 occurs nowhere in the standard. The models' programs were incorrect by the
standard; GnuCOBOL's refusals were right in 64 of the 65 it judged. (PB3488 records the withdrawn reading.)

**Bake-off 2a (qwen3.6 only; items 1–4 with the since-withdrawn `JUDGE_LACKS` list; the run was stopped after qwen3.6 when
MiniMax's replies came back empty).** Usable 11/20 (55 %), THEY_REJECT 0, median 7 s. The 9 failures: FUNCTION TRANSLATE ×2,
FUNCTION NATIONAL, FUNCTION SUBSTRING, HIGHEST-ALGEBRAIC on a literal ×3, two COMPUTE syntax slips — every one a program
the standard refuses, which is what sent the reading to the recount. MiniMax M3.1 Flash (`:cloud`): 1 program compiled in
387 s over four rounds, 1 NO_PROGRAM with empty content after 445 s; the probe that explained it is in PB3488 (28,000
characters of thinking despite `think: false`, every output token).

**Bake-off 3 (21:3x–21:51 PDT; the spec's function list in the prompt, "Argument rules" folding, 275 seeds).**

| Model | Usable | Both refuse | GnuCOBOL alone | Median model s | Lead rows |
|---|---|---|---|---|---|
| qwen3.6:35b | 9/20 (45 %) | 11 | 1 | 12 | 8 |
| laguna-xs-2.1 | 12/20 (60 %) | 8 | 1 | 18 | 11 |
| qwen3-coder:30b | 12/19 (63 %) | 7 | 0 | 20 | 9 |

Its tables showed the seeds the prompt forbids (`OUTPUT_DIFFERS:15.99` WHEN-COMPILED ×2–3 and `15.53` LOCALE-TIME in every
model) and `15.6 Summary of functions` as a seed; the one RUNNER_ERROR was Ollama's "prediction aborted, token repeat limit
reached", the model's degenerate reply. All three fixed before bake-off 4 (PB3488 items 5–7).

**Bake-off 4 (21:5x–22:05 PDT; the finished harness: 265 seeds, three correction rounds quoting both compilers, the
function list and the worked example in the prompt, temperature 0.3).**

| Model | Usable | Both refuse | GnuCOBOL alone | Median model s | Lead rows |
|---|---|---|---|---|---|
| qwen3.6:35b | **14/20 (70 %)** | 6 | 0 | **10** | 7 |
| laguna-xs-2.1 | 12/20 (60 %) | 8 | 0 | 18 | 11 |
| qwen3-coder:30b | 12/20 (60 %) | 8 | 0 | 21 | 6 |

**Reading.** Against bake-off 1 on the same models: qwen3.6 50 → 70 %, laguna 40 → 60 %, qwen3-coder 40 → 60 %, with 20
programs per cell (one program is 5 points, so the direction is firm and the size is not). The repair rounds did most of it
(5 to 8 of each model's usable programs came through a correction). The sample is small enough that bake-off 3's qwen3.6
(45 %) and bake-off 4's (70 %) differ by the seeds drawn, not only by the harness: the failures cluster by clause, since
three variations of one hard clause fail together (SET on a table, the FORMATTED-* date formats). THEY_REJECT stayed at 0–1
per 20: GnuCOBOL 3.2's refusals are the model's slips, not the judge's gaps, which the recount had already said. The
winner for the overnight run is **qwen3.6:35b**: the highest usable share and half the latency of the others.

**Not adjudicated here.** Every survivor row of bake-offs 3 and 4 is a lead for the morning triage (REM ×2–3 in every
model, MIN with national arguments, INITIALIZE, DATE-TO-YYYYMMDD); none has been checked against the standard on this
branch. One is already explained: `WE_REJECT:COBOLNET0900` ×1 in bake-off 4 is DELETE FILE, a 2023 statement the prompt's
own file rule asked for while the compilers run 2014; the branch review (R1-6) found that the prompt's function list was
the 2023 standard's too (CONCAT, SUBSTITUTE, FIND-STRING …), and the harness now reads Annex E.3.3's additions out of
both the prompt and the seeds.

## Decision

The harness lands as tooling (kb/Work PB3463, its yield changes PB3488); the overnight run is the owner's to start, with the
bake-off (`--bake-off`) choosing the generator among the installed models first — on 2026-10-20 it chose qwen3.6:35b. Every survivor of a later run is a lead for a Claude or human
check against the standard; none is a verdict until that check.
