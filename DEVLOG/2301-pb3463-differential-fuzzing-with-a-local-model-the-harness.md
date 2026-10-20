## Entry 2301 — 2026-10-20 14:50 PDT — PB3463: differential fuzzing with a local model — the harness, three smoke runs, the first leads

The owner asked (2026-10-20 10:40 PDT) how the RTX 5090 could do some of the project's work overnight with a local model, and
whether file edits were a fit. The evaluation, recorded in `docs/rearchitecture/evidence/fleet-optimization/2026-10-20-differential-fuzz.md`,
said a local model is useful here only where a machine judges its output: the three earlier records (the 2026-10-03 spike,
PB2886's one-shot, the aider run of DEVLOG 714) each produced confident output an expert had to refute, and an unsupervised
compiler edit is that class of task. The owner: "Build the harness, but we'll try a small test run today."

**Built: `scripts/differential_fuzz.py`.** An Ollama model writes a COBOL program from one spec clause (statements §14.9,
intrinsic functions §15, data description clauses §13.18, arithmetic §8.8 by default; 394 seeds, a statement's structural
sub-clauses folded into one seed); WiseOwl and GnuCOBOL 3.2 both compile and run it through the conformance suite's own
adapters and `Run.operation` (no second copy of the command lines, crash codes, locale pin or hang guard; on Windows the GnuCOBOL
steps cross into WSL through the adapter's hook interface), both pinned to the 2014 edition in free format. A program GnuCOBOL
refuses goes back to the model once with the diagnostics; one it still refuses is `GNU_REJECTS`, the model's failure, dropped.
Verdict classes `AGREE`, `WE_REJECT`, `OUTPUT_DIFFERS`, `WE_FAIL_RUN` and `EXIT_DIFFERS` (same stdout, different exit status), `WE_CRASH`, `WE_HANG` (a compile or a run), `THEY_FAIL_RUN`, `THEY_HANG`, `RUNNER_ERROR`;
survivors grouped by signature into `report.md`; `results.jsonl` appended per program; a `--bake-off` over several models scored
by GnuCOBOL's acceptance; `--self-test` with 30 checks, discovered by `self_tests.py`. The triage rule is printed in every report:
the spec decides, GnuCOBOL is a net, nothing the model wrote enters the register, a golden or the suite without a check.

**The smoke runs.** Run 1 (2 programs) lost both to the harness: a fence tagged "```cob" plus CJK characters that the extractor
refused, and a seed that was the bare "General formats" sub-clause. Run 2 (4 programs) lost all four to output truncation: the
3000-token cap, spent on fixed-form indentation and filler data items, cut every reply before its closing fence. Both fixed in
the tool with self-test checks (any fence string; structural sub-clauses fold into their parent; 6000 tokens; Ollama's
`done_reason` "length" detected and sent back as a request for a shorter program; the prompt forbids indentation and filler).
Run 3 (4 programs, 2 in flight, `qwen3-coder:30b`): 4 of 4 compiled under GnuCOBOL (3 after one repair round), 1 AGREE,
3 OUTPUT_DIFFERS in two signatures, median 24 s of model time per program. The two signatures (DISPLAY of an implied decimal
point after ADD; MOVE of figurative constants into boolean, alphanumeric and national items) are leads for a spec check, not
verdicts.

**The 40-program test run (12:44–12:59 PDT, three in flight).** Its first attempt died in its third program on a harness bug (a
repair round archived a source the first reply never produced); fixed, and with it the rule that a harness exception inside one
program is a RUNNER_ERROR row with its traceback, never the end of the run. The second attempt: 40 programs in 14 min 23 s,
GnuCOBOL accepted 16 (13 only after the repair round), GNU_REJECTS 24 · WE_REJECT 9 · AGREE 4 · OUTPUT_DIFFERS 3, 12 survivors
in 5 signatures (a unary `-` before a separator space, COBOLNET2993 × 6; DISPLAY of a signed item and the `0 ** -1` size
condition; a VALUE literal not representable, COBOLNET1625 × 2; DISPLAY of an implied decimal point; PICTURE 9(34) refused at
the standard's 31-digit limit, a GnuCOBOL extension). All forty seeds came from §8.8 because the seed list was walked in spec
order, so it is now shuffled by default (`--seed`, deterministic; `--in-order` restores the walk). The raw report, the results
and the twelve survivors' sources are under the evidence record's folder. kb/Work PB3463 (analysis) records the decision, the
design and the triage rule; the overnight run is the owner's to start after a bake-off picks the generator.

**Branch review (R1-1 to R1-4) and the survivors' spec check.** R1-1 (blocking): `classify` compared only stdout, so a
WiseOwl run that exited non-zero after its last DISPLAY (a failing CLOSE or DELETE FILE, a STOP RUN status) was AGREE and
never reached the report; the exit status is now compared (`WE_FAIL_RUN`, `EXIT_DIFFERS`, both lead classes; ISO 14.9.42.4
GR 2 makes the termination status observable), with self-test checks. R1-2: a compile that exceeds the 90 s hang guard is an
observation with `hung` set and classifies as `WE_HANG` / `THEY_HANG`, not a runner error. R1-3: the evidence record cited
§8.3.3.3.2 (the literal rule) for the 31-digit item limit, which is §13.18.40.3 SR14; the same inherited clause was in the
compiler's COBOLNET0801 message and catalog entry, now corrected with a unit test. R1-4: the check count (30). The twelve
survivors were then checked against the standard: none is a compiler defect (the evidence record's table has each clause);
the one open question is DISPLAY of a scaled item's decimal point, reported as a lead.

