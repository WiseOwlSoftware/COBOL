## PB4006: a cumulative differential-fuzz history, and a machine-generated page that each night republishes

The owner asked for a cumulative summary of the fuzz nights, published as an Artifact, written for a reader new to the
project, and maintained automatically as part of every run. Until now each night's numbers lived in its git-ignored run
directory and its evidence record's prose, and its verdict files in session scratch, so no one could see the trend.

- `conformance-suite/fuzz-history.json` is the new tracked record, one entry per night. It holds
  run.json, report.md's headline, the harness classes, the adjudicators' verdict tally and the night's WiseOwl note ids. It
  holds no statuses. It was backfilled for nights 1 to 7. Nights 3 to 7 have tallies, taken from the verdict files that
  survived in the triaging sessions' scratch; nights 1 and 2 were triaged before verdict files existed.
- `scripts/fuzz_history.py render` draws the page from the history and the register: the totals, programs per night, WiseOwl
  defects per night, who was right, and every defect with its current status. The charts are inline SVG.
- `scripts/fuzz_triage.py history --triage <dir> --judge-commit <sha> --write` is the morning's new last step. It records the
  night and writes the page, and the session publishes the page to the URL the history carries:
  https://claude.ai/artifact/UBCptoMp1GohHrvpUFRS7g.
- The design is updated in `DESIGN-differential-fuzz.md` §6, where tests are now §7 and alternatives §8. The Workflow's step
  list adds step 7, and `scripts/test_fuzz_history.py` is the new self-test.

So far, seven nights have judged 39,906 programs, and the compilers disagreed on 11,307 of them. Twenty-four WiseOwl defects
were found (21 landed, 1 retired, 2 open: PB3996 and PB3997), and the register holds 73 GnuCOBOL records.
