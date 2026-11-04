## The 3,000-program Sonnet fuzz batch, filed: 47 GnuCOBOL records, 33 witnesses, and fuzz night 10

The GnuCOBOL side of the first large Claude-written fuzz batch (kb/Work PB4506; its WiseOwl defects are PB4511-PB4517).
Fourteen Opus adjudicator slices recorded 49 GnuCOBOL-side mechanisms as records; nine independent refuter slices
re-ran each one on cobc and upheld 45, reclassified 2 and overturned 2 (`open-no-rewind-not-implemented`, where Annex A
makes the behavior the implementor's, and `alphabet-locale-not-supported`). `fuzz_triage.py file` wrote the 47 that
survived as `upstream` notes PB4519-PB4565, each carrying its minimal program, the cobc command and the refuter's checked
citations, and added this run's programs as witnesses to 33 existing upstream notes. Three families two slices had
recorded twice were merged first (one record per family, design §4.1).

The night is recorded as fuzz night 10 (`fuzz-batch-3000-20261103`, judge built from main 3c8b2845c) in
`conformance-suite/fuzz-history.json` and the ledger. Triage and refutation cost $86 of the expiring API credit
($0.24 a signature to adjudicate, $0.39 a record to refute).

The run id first came out as `judged`: `fuzz_batch.py collect` prints a judge command whose output directory is
`DIR/judged` for every batch, and the triage takes the run id from that directory's name. This run's directory was
renamed to the batch's own name before anything was recorded; the command is fixed separately.
