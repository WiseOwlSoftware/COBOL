## PB5759: the tracked record of a code read at an edition, and the first import of 1985 reads

**Why.** Kb/Work PB5692's ruling (2026-11-15) makes every KEPT row of 1985 owe a verdict adjudicated on the code at
`--std 85`, and the reads were starting on the other account with nowhere to land: `audit_batch.py import-reads` refused
the sample's files (`(no rule-id): the record holds no reader's 'cases'`), a lineage read record holds only a pairing of
texts (SAME, DIFFERENT, DOUBTFUL), and a KEPT row holds no verdict of its own. The re-probe on this tree repeated the
recon's refusal; nothing about the compiler is in question here (the change is the inventory's tooling).

**What landed.** `scripts/spec/code_reads.py` with its view half `scripts/spec/code_read_store.py` (the store, the outcome
and the counts, which `inventory_view` imports; the shape checks' closure is no input of the conformance-suite generator,
so BUILD TARGETS refused one file): the store `tests/version-matrix/code-reads-<year>/<rule-id>.json`
(written through `inventory_store.write_store`), the record (the reader's record and the refuter's case verbatim, the
rule digest, the pin, the slice, the digests of the source files and of the parts held), the import of the pipeline's
files as they are (every slice or none, every problem named by slice, rule and field), the one outcome function
(confirmed, contradicted, undecided, stale), `work`, `adjudicate`, `plan` and `--check`, the gate's CODE READS audit (and a
step of CI's audits job). The required fields are the reader brief's; the verdict vocabulary is the schema's, the
code-location shape the inventory's (`witness_shape_error`), each citation parsed by `citation.py`, of the rule's own
edition (or the amendment's, as the catalog's own citation names it) and checked by `cite.resolve`. That each site still
resolves is the battery's, by the one predicate a row is held to: `SpecTraceabilityInventoryDriftTests.
EveryCodeReadLocation_ResolvesInTheTree`. `inventory_view.py --summary` prints a line of its own for an older edition with
a store; no row's state changes (`--states --edition 1985` sha256 `d192c6f0…fcbb` before and after). The self-test is
`scripts/selftests/spec/code_reads/test_code_reads.py` (8 cases, every refusal and every outcome on planted reads). Design
section 10.6.1 is new; kb/Work PB5683 gains a dated file with the store's commands and the correction that the pairing
check does not take the stored record's targets.

**The first import.** `import --edition 1985 --reads E:/COBOL-coord/scratch/sample-shared/out` refused the whole sample:
eleven rows of slices 1985-01, 1985-07, 1985-09 and 1985-10 write a note in parentheses inside a code-location
(`IntrinsicCatalog.cs#IntrinsicCatalog (TAN row: ...)`), which is no inventory shape. The files were not edited and the
importer was not loosened; the six clean slices, copied byte for byte, gave 36 records at pin `d615406082` (the reading
tree's head while the sample ran, from its reflog): 31 confirmed, 4 contradicted (`1985-GR-XII.2.4-8` PARTIAL for PB5732
and `1985-SR-XI.4.4.3-10`, `-4` PARTIAL for PB5731, adjudicated from `kb/Work/PB5692/2026-11-15-adjudication-of-the-sample.md`;
`1985-SR-VI.6.27.3-4`, the reader's PARTIAL, PB5734), 1 undecided (`1985-SR-VI.5.8.3-1`, PB5737 open). 1,338 KEPT rows are
owed a read, 535 witnessed.

**Gate.** See the branch's report for the verdict line.
