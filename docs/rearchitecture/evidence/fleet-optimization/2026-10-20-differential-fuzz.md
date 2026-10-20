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

## The 40-program test run

(Appended when the run completes; `--count 40 --jobs 3`, default seed prefixes 14.9, 15, 13.18, 8.8.)

## Decision

The harness lands as tooling (kb/Work PB3463); the overnight run is the owner's to start, with the bake-off (`--bake-off`)
choosing the generator among the installed models first. Every survivor is a lead for a Claude or human check against the
standard; none is a verdict.
