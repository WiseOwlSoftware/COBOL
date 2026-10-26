## PB3795 + PB3375: a verdict record no longer erases a row's notes, and the audit sees an erased statement

`record_verdicts.merged` took a full record's statement fields from the record and wrote an absent one as empty. A record
that restated a verdict and forgot `notes` therefore erased the row's forensic prose (the derivation, its `cite.py --check`,
its goldens) with no message, and nothing could see it: `audit_witness_loss.py` watched `code-location` and `test-ref` only,
and the retirement marks the writer appends kept `notes` non-empty on the rows that retired a witness.

**Re-probe.** On this tree `merged(GR-4.2.10-1, {rule-id, verdict, editions})` took `notes` from 2025 characters to 0 and
`validate` was silent. Extending `audit_witness_loss.py --history` to statements showed the loss was not the two landings
PB3795 named but four (PR #541, #599, #628 and the grammar/OO merge of 2026-09-30) and 25 rows still blank: the 16 of
w20261505d, SR-14.9.22.3-1/2, SR-14.9.43.3-1, SR-14.9.48.3-2, SR-13.8.6.2.3-1/2, GR-14.6.11-3/4 and GR-14.9.42.4-1. PB3375
(open since 2026-10-18) was the same defect and is landed with this change. The branch tips carried the blank, not the
keyed-inventory merge driver.

**Fix, one root.** A statement field (`inventory_schema.STATEMENT_FIELDS`: verdict, editions, notes, derivation) is the
record's when the record names it and the row's when it does not, the rule the witness fields already had. One definition of
an erased statement, `inventory_schema.statement_losses` (the `notes` PROSE, not the marks behind it; a `derivation` replaced
by a spec-derived `test-ref` is the one excuse), is asked by `validate` (a record writing a statement empty over prose is
refused, naming the field; a record that changes the verdict must restate `notes`) and by `audit_witness_loss.py --check`,
which the gate and CI already run and which is now RED on an erased statement. `--history` lists every erasure and
`--restore-batch` emits full records that give erased `notes` back, keeping the marks the row carries now after the prose.

**Siblings.** `normalize_batch.normalize` wrote `notes: ""` into every record that had none and `grammar_findings_to_batch`
named `notes: ""` for a MATCHES finding; both now omit it. `derive_verdict_batch` carries the row's notes forward and only its
comment changed. `audit_witness_loss.py --history` also crashed on every pre-PB3172 inventory schema; `schema_at` now answers
"cannot read" and the callers fall back to the current schema, as they already did for a missing one.

**Branch review round 1.** (R1-1, blocking) `statement_losses` excused an emptied `derivation` only when the row had ANY
`test-ref`, while `merged` now keeps an omitted one: a derivation-closed rule found defective could be recorded neither
way (kept: `verdict-does-not-resolve`; written empty: erased statement; hand edit: RED in the audit). The excuse is now
`Schema.derivation_superseded(after)`, the first two refusals of `Derivation.refusals` (a SPEC-DERIVED test, not any
test-ref; or a verdict that does not resolve), so the one place that lets a derivation go is the one that refuses it
staying; `statement_losses` takes the schema. A record that omits `derivation` under a defect verdict is refused with the
way out spelled in the message. Probe: GR-4.4-1 to PARTIAL with `derivation: ""` is accepted by `record_verdicts --dry-run`.
(R1-2, lead, in a file already changed) `--history --restore-batch` put erased `notes` back under the row's current
verdict; `Erasure.verdict` records the verdict the prose argued and `owed_statements` strands (reports, never restores)
a row whose verdict has changed since. Self-test +4 checks (differential test-ref is not an excuse, defect verdict is,
the writer's two outcomes for the real derivation-closed row, the stranded restore).

**Rows.** The 25 notes were restored from the commit before each loss (batch `pb3795-restore-erased-notes`); no verdict,
witness or GAP count moved (`closes_rows: []`).

**Goldens.** `audit_witness_loss.py --self-test` plants an emptied note, a marks-only note, a rewritten note, an emptied
verdict, editions and derivation (with and without a replacing test), then asks the writer (omitted notes kept, named notes
restated, empty-over-prose refused, verdict change without notes refused, witness-only accepted), the history restore and
`normalize_batch`; `grammar_findings_to_batch.py --self-test` covers the MATCHES record.

**Self-review.** Silent-failure lens: the loss was silent because absence and emptiness were one value; the rule now
distinguishes them and the audit closes the hand-edit path. The gate's verdict line is in the report.
