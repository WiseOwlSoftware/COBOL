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

**Review round 1.** A refusal or a flag now witnesses only a syntax rule or general format (the evidence table, data) or a
declined facility whose read states its 2002 licence; any other rule needs a case accepted at 2002. A KEPT record
carries exactly the candidate pairing's targets, and a SAME for an unpaired rule becomes the work item
`own-verdict-from-pointer`. Any slice label imports, and an empty `to` too. `widen --apply` keeps only widenings proven
in that run and restores everything on any failure. A rule read twice is refused before any write, and `load` verifies
each record's digests. The gate's hang of `pb739_shared_extend_write_2023` is filed as kb/Work PB5626.

**Review round 2.** `widen --apply` proves each candidate on an in-memory band through the generator's own check and the
suite's runner, and writes only the proven widenings, last, so a killed run leaves nothing unproven and a re-run
finishes it. An uncommitted band is no witness. A `licence-2002` must open with a 2002 citation that the citation
checker resolves. A syntax rule whose text grants a form needs an accepted case (a pattern list in the evidence table
that fails closed).

**Measured (dry runs: no 2002 record is written).** The import read 69 slices covering 1,720 rules. Before the evidence
check, 300 rules would be kept. 632 were refused because a screen flag is unanswered. 788 became work items: 407
needs-own-witness, 217 defect, 108 undecided, 55 own-verdict-from-pointer and 1 uninterpretable. `change-batch` wrote
204 KEPT records and refused 728, and `record_verdicts.py --edition 2002 --dry-run` accepted all 204 (GAP 3,297 to
3,094). Of the 96 rows refused only for their evidence, `widen` found 66 widenable and 30 that need their own witness.
For 1985, 802 of the 1,373 stored KEPT records have a witness of the kind the rule needs at 85 for at least one target,
and 571 have none.

**Not done.** The real import, the band widening and the 2002 records are the records worker's. So are the answers
to the 632 rows whose screen flags their slices never asked, and the own verdicts of the 55 unpaired SAME reads. 1985's state rules and records
are unchanged.
