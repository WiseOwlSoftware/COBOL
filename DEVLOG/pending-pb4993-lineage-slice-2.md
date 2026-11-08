## PB4993 lineage slice 2: the change record is schema data, the 1985 catalog carries each rule's obsolete standing, and the audit's 759 upheld SAME rules are recorded as KEPT

Wave 20261929, group A. The second slice of the rule lineage (kb/Work PB4991; DESIGN-spec-conformance-review.md
section 10.2), after PB4992 made the inventory a view.

**Re-probe.** On the base (4390441ee5) `lineage.py kept-batch` over the audit run `audit-1985/state-route` wrote 759 KEPT
records and `record_verdicts.py --edition 1985 --dry-run` validated them (GAP 2,253 -> 1,556). Three things were still to
do: `next`'s kinds and keys were `lineage.py` constants, the 1985 catalog had no `status`, and the batch was unrecorded.

**The change record is data.** `inventory-schema.json` gains `next`: the keys a change record may carry, the fields that
state a verdict, and each kind as three flags (does it name `to` rules, must it state its `reason`, is its verdict its
chain's). `inventory_schema.ChangeRecord` reads it, refusing a schema with other than one chained kind;
`lineage.next_errors`, `lineage.state_of` and the new `lineage.chained` read it through `Schema.next`, so no code spells a
kind, and record_verdicts and the view's audit pass the schema. The audit's disposition vocabulary (CARRIES, OWN-ROW,
DISPUTED, UNVERIFIED, OPEN) is named once in `audit_batch.py`. The lineage self-test plants a kind the schema alone
adds, a schema with two chained kinds, and a schema from before `next`.

**The obsolete standing.** Each 1985 catalog row now carries `status`: `normal`, or `obsolete: C.1(n)` naming the entry
(or entries) of the standard's own list, §1985:C.1, which §1985:I.1.5.2.3 defines. The list is held once as data,
`docs/rearchitecture/spec-obsolete-elements-1985.json` (per entry: its number, the terms that name it, the sections that
specify it; the name is read from the list itself), and `scripts/spec/obsolete_elements.py` applies it as the Mythos
session decided after review w20261927a: per section, OWN when the heading names a term (every rule marked), else SHARED
(only the syntax and general rules naming a term; the general format stays current). The drift checks run in the RULE
CATALOG 1985 audit: the entries are exactly the list's 18 items; each section is a locator of the text; each of the
text's "is an obsolete element" sentences falls in a section some entry names, or in one of the three sections that
describe the level tables' Z notation; each entry resolves to a rule (entry 1, double character substitution, is prose
outside any rule block and is declared `no-rule`, checked both ways); and no shared format or out-of-section rule is
marked. 143 rules are obsolete; the audit finds nothing.

**The kept batch is recorded.** The batch is the audit run's own output now, `audit_batch.py kept-batch` beside
`results` (`kept_records`, from the run's dispositions, with no empty `reason`: a store holds only what says something),
and `record_verdicts.py --edition 1985` recorded the 759 KEPT records under
`tests/version-matrix/inventory-1985/`. The 1985 view is 2,253 rules, 1,556 GAP. 62 KEPT rules stay GAP because the 2023
rule each carries into has a stored band without `85` (STRING's general rules among them), which slice 5 (PB4996)
derives. The obsolete standing cross-checks the audit: 142 of the 143 obsolete rules are its OWN-ROW, and the one KEPT
(`1985-GR-XVI.2.2.4-1`) is the Segmentation module's restatement of a current OBJECT-COMPUTER rule.

**Self-review.** Architecture: one module per job (the shape in the schema, the standing in `obsolete_elements.py`, the
catalog wiring in the 1985 arm); OWN or SHARED derived, never held. Duplication: the kind names, the disposition names
and the verdict fields each have one home. Code: the marking implements the decision's per-section rule, and the drift
checks are the decision's four plus a both-ways check of `no-rule`. Performance: the marking is 18 entries times their
sections over 2,253 rows, once per catalog run.

**Two gate reds, two roots.** Run 000001 was RED at WITNESS LOSS: that audit joins main's 2023 inventory under main's
own schema, which has no `next`, and `Schema.__init__` refused it. Only the lineage asks for the change record, so
`Schema.next` is now read when first asked, and the lineage self-test plants a schema without it. Run 000002 was RED at
INERT LANE: the kept batch lived in `lineage.py` and imported `audit_batch`, so the view a compiler test runs reached the
API-key tooling. The batch moved to `audit_batch.py`, which imports the schema, never the reverse; its self-test
(`test_audit_batch.py`) checks the records are the CARRIES rules only and legal change records.

The gate at the branch head is the third run; its verdict line is in the wave report and the gate record `enqueue.py land` reads.
