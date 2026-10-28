## Entry 2706 — 2026-10-28 14:35 PDT — PB4006: a cumulative differential-fuzz history, a machine-generated page and FUZZ-LEDGER.md, refreshed every night

The owner asked for a cumulative summary of the fuzz nights, published as an Artifact, written for a reader new to the project
and maintained automatically as part of every run. They then asked for the same summary at the repository root, "so I can
easily link to it and track readers", and chose Markdown with SVG charts: GitHub shows a committed `.html` file as source, never
as a page. Until now each night's numbers lived only in its git-ignored run directory and its evidence record's prose, and its
verdict files in session scratch, so no one could see the trend.

- `conformance-suite/fuzz-history.json` is the new tracked record, one entry per night: run.json, report.md's headline, the
  harness classes, the adjudicators' verdict tally, and the notes the night filed on the WiseOwl side. The notes are found by
  one rule: every defect or adjudication note whose `source` names the run. The file holds no statuses and no kinds. It was
  backfilled for nights 1 to 7, with tallies for nights 3 to 7 from the verdict files that survived in session scratch.
- `scripts/fuzz_history.py` writes one text and draws it twice. `render --out` produces the HTML page for the Artifact
  (https://claude.ai/artifact/UBCptoMp1GohHrvpUFRS7g). `render --markdown` produces `FUZZ-LEDGER.md` at the root, with
  standalone SVG charts in `docs/fuzz-ledger/`. Each note's kind and status are read from kb/Work when the pages are drawn: a
  compiler bug, a test-harness bug, a settled question, or a retired finding. Only compiler bugs reach the totals.
- `scripts/fuzz_triage.py history --triage <dir> --judge-commit <sha> --write` is the morning's new last step. It refuses an
  unfinished triage or an unreadable report, records the night (a re-run keeps its number, notes and judge), and writes the
  page, the ledger and the charts. The session then publishes the page.
- `DESIGN-differential-fuzz.md` gains §6, so tests are now §7 and alternatives §8. The Workflow's step list gains step 7, and
  `scripts/test_fuzz_history.py` is the new self-test.

The branch review (round 1, finding R1-1) found that the backfill's hand-copied note lists were not the tool's rule, and that
the totals counted a harness bug, a settled question and a retired finding as WiseOwl bugs. Every night is now rebuilt by the
rule and classified from the register. The four review leads are fixed too:
- the stated facts are derived from the records;
- the `history` subcommand has tests;
- an unreadable report is refused;
- only GnuCOBOL records the fuzzing produced are counted.

To date: seven nights, 39,906 programs judged, 11,307 disagreements, 23 compiler bugs found in WiseOwl (21 landed; PB3996 and
PB3997 open), and 72 GnuCOBOL records.

