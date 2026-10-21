## PB3488: the differential fuzz harness's yield, measured twice and corrected once

The owner ran the harness's first bake-off tonight (five Ollama models, 20 programs each; kb/Work PB3463's `--bake-off`)
and read the table as "the models majoritively generate incorrect programs. Can we improve that?" The best model,
qwen3.6:35b, had 10 of 20 programs accepted by GnuCOBOL; the worst, 3. The owner then said "Do items 1 through 4 and
rerun the bake-off". This entry is the branch `fuzz-yield-pb-20261020` (kb/Work PB3488), and it records a wrong reading
as carefully as the fixes, because the wrong reading nearly shipped.

**The wrong reading.** A tally of GnuCOBOL's messages over the 48 refusals that carried an `error:` line said `FUNCTION
'TRANSLATE' unknown` ×4, `FUNCTION 'NATIONAL' unknown` ×2, `FUNCTION 'HIGHEST-ALGEBRAIC' has invalid argument` ×6, `COMP-3 …
reserved word in another dialect` ×1, and I told the owner that about two fifths of the refusals were the JUDGE lacking a
2014 feature, not the model. The first version of item 3 was a hand-written `JUDGE_LACKS` list naming those four. The
measurement that should have come first came second: every program GnuCOBOL refused in the first bake-off, compiled by
WiseOwl. **Of the 65 refused programs, 1 compiles** (2 more refused rows held no program). TRANSLATE, NATIONAL and
SUBSTRING are not intrinsic functions of ISO/IEC 1989 (WiseOwl's
COBOLNET1501 cites §15.6, the summary of functions); COMP-3 occurs nowhere in the standard; HIGHEST-ALGEBRAIC's argument
"shall be a data item of category numeric or numeric-edited" (§15.43.3 argument rule 1, `cite.py --check` OK), and the
models passed literals. The models' programs were incorrect by the standard, and GnuCOBOL's refusals were right in 64 of
the 65. The lesson is the one the memory already carries ([[a_remembered_pattern_is_a_hypothesis]]): a compiler's message says
what IT refused, never whether the standard agrees. The list was withdrawn; the prompt now carries the standard's OWN
function list, read from the `15.n NAME function` headings of `specs/ISO_COBOL.md` (94 names; `spec_functions()`), and a
self-test holds that list to contain REVERSE and HIGHEST-ALGEBRAIC and none of the invented names.

**What the harness does now (`scripts/differential_fuzz.py`).** A program GnuCOBOL refuses is compiled by WiseOwl before
anything else: one WiseOwl compiles is a `THEY_REJECT` lead (GnuCOBOL lacks the construct, or WiseOwl under-rejects; the
spec decides at triage) and is not sent back, because a correction would replace the construct under test; one both
refuse goes back with both compilers' first errors (GnuCOBOL's first six, WiseOwl's first four with their § citations),
three rounds (`--repairs` 3, was 1); one still refused is `BOTH_REJECT`, the model's failure; a reply with no program is
`NO_PROGRAM`. `GNU_REJECTS` is gone. The prompt carries one worked example both compilers accept (compiled and run by both
before it was written in; the self-test holds it to every rule the prompt states), the function list, the rule that
HIGHEST-ALGEBRAIC takes a data item, "close every conditional statement with its END- word", and the temperature is 0.3.

**What the measurements found in the harness itself.** Three defects, each fixed at its root with a drift test where one
fits. (1) The seed folding knew "Arguments" and "Returned value(s)" but the spec writes "Argument rules" (45 headings),
"Returned value rules" (49), "Returned value rule" (43), "Argument rule" (37) and five rarer spellings, so every function's
rules sub-clauses were seeds of their own, titled without the function's name; MiniMax's visible thinking said it plainly
("clause 15.66.3 likely NATIONAL-OF? Need infer"). Fixed case-insensitively with the spellings counted from the spec;
394 → 275 seeds; a nested seed now carries its ancestors' titles ("PICTURE clause > Editing rules"); the drift test fails
on any nested seed whose title is a bare structural one. (2) The clauses of the functions the prompt forbids (WHEN-COMPILED,
CURRENT-DATE, LOCALE-TIME, RANDOM …) were seeds, so 3 to 4 programs in 20 asked for output two compilers cannot agree on;
`NONDETERMINISTIC_FUNCTIONS` is now one definition the prompt's rule and the seed filter both read, every name held to the
spec's function list; 15.6 "Summary of functions", a table, is not a seed either; 265 seeds. (3) The final WiseOwl compile
re-staged a program into the work directory a correction round had left, and `Run.stage` refuses an existing directory
(`FileExistsError`, 3 of 16 programs lost in the first run of the branch); `observe_fresh()` is now the one mechanism for a
fresh observation, and a directory that cannot be removed raises into a RUNNER_ERROR that names the cause. Also: Ollama's
HTTP 500 "prediction aborted, token repeat limit reached" is the model's degenerate reply, counted against it, not a
RUNNER_ERROR; and a NO_PROGRAM row whose reply spent every output token thinking says so.

**The branch review (one read-only reviewer, round 1: 4 blocking findings, 4 leads, all answered).** R1-1 (high): a
WiseOwl compiler CRASH on a program GnuCOBOL also refused was recorded as "did not compile", sent back for a correction
and lost; the rounds now end on a crash (WE_CRASH), and a self-test drives the program loop with a stubbed model and
compilers. R1-6 (medium), the most instructive: the seeds are the 2023 text, the compilers run 2014, and the new "ONLY
intrinsic functions" rule listed CONCAT, CONVERT, FIND-STRING, SUBSTITUTE, BASECONVERT, MODULE-NAME and
SMALLEST-ALGEBRAIC, which WiseOwl refuses under `--std 2014` (COBOLNET1502); the prompt's file rule asked for DELETE FILE,
2023 too, and bake-off 4's one COBOLNET0900 row was that. The harness now reads the standard's own list of what 2023
added, Annex E.3.3's items that "has been added" (`cite.py --check E.3.3` OK on items 15 and 23), out of the function
list and the seeds and names them to the model as errors. R1-2 (medium): the drift test over the real spec filtered with
the very pattern under test and could never fail; it has its own word list now. R1-3 and the leads: PB3463's text, the
recount's arithmetic (67 refused rows, 65 with a program, 1 of 65 compiles; the raw tally is kept), the bake-off column
named for what it counts, a return annotation.

**MiniMax M3.1 Flash** (`minimax-m3.1-flash:cloud`, Ollama's US-hosted model, free until 2026-10-22): the owner proposed
it mid-evening. Through the same local API it honours `think: false` on a 60-token prompt (0.9 s) and ignores it on the
harness's 1,455-token prompt: 28,000 characters of thinking, every one of the 6,000 output tokens, 69 s, empty content,
with and without the field. Its one compiled program took 387 s over four rounds. Unusable here until Ollama honours the
field for it (a per-model `NUM_PREDICT` is a lead). What it receives is the system prompt and one clause of the spec
transcription, which is tracked in the public repository.

**The measurements.** Usable programs of 20, bake-off 1 (the PB3463 harness) → bake-off 4 (this branch), same three models, same
seed: qwen3.6:35b 10 → 14 (median model time 7 → 10 s), laguna-xs-2.1 8 → 12, qwen3-coder:30b 8 → 12 (39 → 21 s).
THEY_REJECT 0 to 1 in every cell. Five to eight of each model's usable programs came through a correction round, so the
rounds carried most of the gain. One program is 5 points: the direction is firm, the size is not, and bake-off 3's qwen3.6
(9 of 20, before the forbidden-function seeds were removed) against bake-off 4's 14 says the seeds drawn matter as much as
the harness. The winner for the overnight run is qwen3.6:35b, the highest share at half the others' latency.

**Gate.** `BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` at 8e80f29fb (21:5x PDT) and again at the
review-answer head; `--self-test` 30 → 54 checks. The evidence record `docs/rearchitecture/evidence/fleet-optimization/2026-10-20-differential-fuzz.md` carries the
tables and keeps every bake-off's scoreboard and report in its raw folder. The overnight run is the owner's to start with
the table's winner.
