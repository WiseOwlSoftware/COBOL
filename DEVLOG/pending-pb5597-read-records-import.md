## PB5597: the 2002 read decisions become records only through an audit run, on evidence at their own edition; PB5589's review leads R2-1 to R2-4 fixed

**What landed.** `audit_batch.py import-reads` takes a directory of a reading fleet's `<slice>-read.json` and
`<slice>-refute.json` files and does two things. It writes the durable record of each slice, verbatim with its questions
and its sources' digests, under `tests/version-matrix/lineage-reads-<year>/`, so every change record can name the read
that justifies it. It also starts an audit run whose map pass is each reader's answer and whose verify pass is each
refuter's; no model is asked and no Message Batches call is made. `change-batch --state` then writes a KEPT record only
for a SAME that its refuter upheld, with every lineage-screen flag answered for its detail and, for 2002 (the schema's new
`own-edition-evidence`), a conformance-suite witness at `--std 2002` for each rule it carries into. Each other record is
refused alone and listed with its reasons. A DIFFERENT, an overturned SAME and a DOUBTFUL read become work items
(`needs-own-witness`, `defect`, `undecided`), never records. `scripts/spec/lineage_reads.py` holds the one normalization of
the reads (it refuses a record it cannot read, by slice and rule), the decision rules, the band-widening command (`widen`,
a dry run unless `--apply`), the 1985 measurement (`own-edition`) and the new LINEAGE READS audit (gate and CI). PB5589's
review leads are fixed at their roots. `next.answers` now names the flag detail it answers (R2-2). `record_verdicts.py`
checks every change record against the screen, so a flag that appears on a stored record later is owed a re-read, not a
red gate (R2-1). The JSON backspace in the communication-reach pattern is now `\\b`, and a control character in a trigger
parameter is refused at load (R2-3). Answers travel into the change, and one bad row no longer refuses its whole batch
(R2-4).

**Measured (dry runs: no 2002 record is written).** The import read all 63 slices, covering 1,570 rules. Before the
evidence check, 291 rules would be kept. 664 were refused: 531 because a flag is unanswered and 133 because no refuter
record exists. 615 became work items: 320 needs-own-witness, 207 defect, 87 undecided and 1 uninterpretable.
`change-batch` wrote 215 KEPT records and refused 607, and `record_verdicts.py --edition 2002 --dry-run` accepted all 215
(GAP 3,297 to 3,083). Of the 76 rows refused only for their evidence, `widen` found 58 widenable and 18 that need their own
witness. For 1985, 887 of the 1,373 stored KEPT records have a witness at 85 for at least one target, and 486 have none.

**Not done.** The real import, the band widening and the 2002 records are the records worker's. So are the re-read of the
531 rows whose screen flags their slices never asked and the 133 SAME reads with no refuter. 1985's state rules and records
are unchanged.
