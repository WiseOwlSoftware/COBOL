## Entry 3545 — 2026-11-12 14:11 PST — PB5067: a draft carries its library texts, and 15 COPY witnesses of 1985 are placed

**What was wrong.** A draft of the 1985 audit had no place for the library texts its COPY statements name. `run_drafts.py` wrote every test with `"files": []`, so each of the 15 COPY drafts of PB4995's triage ran MISMATCH with "copybook not found". `place_witnesses.py` skipped any draft whose program held a COPY, a guess read from the program text. That guess also skipped `1985-SR-XII.2.3-10`, whose only COPY lies in an AUTHOR comment-entry. The harness staged a declared file flat, so a text in a library folder lost its library. The in-process runner keeps the folder.

**The fix (kb/Work PB5067).**
- `audit_batch.draft_problem` is the one shape of a draft. It covers `files`: `<text-name>.cpy` or `<library>/<text-name>.cpy` mapped to its text. The drafts prompt now asks for those texts.
- `run_drafts.tests_of` writes each draft's texts beside its program and declares them on the test and on its control, so the runner and the placement both pass them through `harness_run`.
- `run.py` stages a declared file at its place beside the source, as `ConformanceSuiteRunner.Stage` does.
- `place_witnesses.py` writes a placed draft's texts beside its test with a `files` header. It refuses a name the clause folder holds with other text, and it does not place a draft whose texts lie in a library folder (only a WiseOwl test may declare one).
- The generator's reader reads an 85 comment-entry as a comment, so a COPY in it is no COPY statement (§1985:XII.2.3 SR10).
- The self-tests carry a COPY draft that fails without the fix.

**Used.** A drafts pass of 21 drafts (the 16 COPY drafts and the five warning-only refusals) went from `{'MATCH': 6, 'MISMATCH': 15}` before the fix to `{'MATCH': 21}` after it, on cobol 25.0.0. Five drafts were given a library text-name of their own because their clause folder holds one file per name. Results:
- placed 15 (11 output, 4 refusals);
- 14 witness-only `test-ref`s recorded at 1985 (1985 GAP 321 → 321: a witness closes no row without its code read);
- 1 placed on a KEPT chain;
- 1 skipped (`1985-SR-IV.4.3.8.1-L2.7`, a library folder).

The five warning-only refusals (the four USE statements' GR2 and `1985-SR-XV.3.2.3-2`) are no under-rejections: "The results of violating the formats or rules of Standard COBOL are undefined unless otherwise specified" (§1985:I.1.6). A 1985 compiler may accept them with WiseOwl's warning. Their witness is a flagged test, which waits for the clause pass, so they are held. kb/Work/PB4995/2026-11-12-drafts-85n.md has the counts.

