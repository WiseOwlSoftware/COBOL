## PB5822: a chained (KEPT) row of an older edition closes on a derivation of its own

kb/Work PB5822 (a decision, filed in this change) lets a chained row whose rule no program can witness close on a
derivation OF ITS OWN (DESIGN-spec-conformance-review.md section 1.1). Before it, `lineage.state_of` and the C#
`StateAt` closed a KEPT row only on a spec-derived test-ref of its own, and the schema's `next.verdict-fields` listed
`derivation`, so `lineage.next_errors` refused one there: the twenty KEPT rows of 1985 that seven decision notes hold
unwitnessable could never close.

**The mechanism.** One predicate per engine, `lineage.derivation_refusals` (Python) and `DerivationRefusalsAt` (C#):
`Derivation.refusals` judged under the verdict the row is judged under (`lineage.chain_of` / `ChainOf`: a chained row's
chain, None for every other row). Refusal 2 keeps its stable code `verdict-does-not-resolve` and, for a chained row,
reads "its chain is not closed"; every other refusal (the computed anchor, refusal 1, the register row, signature,
arm, the UPHELD refuter record, the arm's check against the row's OWN edition's catalog) applies unchanged. A target's
derivation is never read. `lineage.own_evidence` (a spec-derived test-ref of its own, or its own derivation standing)
is what `state_of` and `unwitnessed` ask; `StateAt` mirrors it. `next.verdict-fields` is now `verdict` and
`code-location`.

**Its askers.** The writer: `record_verdicts.validate`'s chained branch now asks the predicate (it used to `continue`
past the derivation block) and `statement_losses` (a chained row's statements could be erased by the writer before).
The audits: `audit_derivations.py --check` judges each held edition's rows at their own edition against the next
edition's view rows; `audit_witness_loss.py --check` passes each older row's chain to `statement_losses`, whose
derivation excuse (`Schema.derivation_superseded`) would otherwise have excused every chained row's lost derivation on
its empty verdict. The planners: `lineage_reads.bare_kept` leaves out a chained row the view computed OK with no
witness of its own (the one line changed there). C#: `EveryDerivation_StandsUnderItsOwnArm` and
`TheRowsClosedOnADerivation_AreTheEnumeratedPopulation` judge each edition's rows at their edition (`StatesOf`), so a
chained row closed on a derivation is in the measured population and its refusals are its chain's.

**Tests, all planted (no real row is recorded).** `lineage.py --self-test` (9 new cases: OK on a standing derivation,
the own-edition catalog check for both catalog arms, refusal 1, the open chain, next_errors, never inherited, the loss
rule under the chain); `audit_witness_loss.py --self-test` (the writer at 1985: accepted, three refusals, the
withdrawal; the WITNESS LOSS audit red on an erased chained derivation); `test_reads.py` case 05c (bare_kept and
unwitnessed leave out the derivation-closed row and keep one whose derivation does not stand); six chained cases in
`derivation-parity-cases.json`, evaluated by both engines with refusal codes compared, and two new
`audit_derivations.py --self-test` checks that the chain is load-bearing; the C# fabricated-inventory self-test.
Reverting `state_of` to the old rule turns the lineage self-test, `test_reads.py` and the parity check red.

**Unchanged real stores.** No row carries a chained derivation (counted: 0 in every edition), the 1985 `--states`
output is byte-identical before and after, `audit_derivations.py --check` reports no findings over 52 claimed
derivations, and `lineage_reads.py --check 1985` and `--check 2002` are GREEN.

The design document's sections 1.1, 8.2, 10.2 and 10.6 state the rule; the schema's `next` `$comment` and its
`$refusals` item 2 too.

**Second mechanism, kb/Work PB5783 option 1: a rule that withholds a property is witnessed by a refusal.** The
evidence table (`scripts/spec/data/lineage-evidence-kinds.json`) gains `withholding-rules`, one pattern: a CLAUSE or
PHRASE that "does not imply" something. `EvidenceTable.diagnosable` is now: no `needs-accepting-case` pattern matches
(that fail-closed heuristic still wins) AND (a diagnosed kind OR a withholding pattern matches); `load` refuses the key
missing or a pattern without its why. Measured on the held catalogs: a bare "does not imply" matched two rules per
edition, and at 1985 it would have made the OCCURS DEPENDING ON rule `1985-GR-VI.5.8.4-2` diagnosable (its "This does
not imply" glosses a rule with observable results), so the pattern names the clause as the subject. With it the new
key makes exactly `1985-GR-X.4.5.4-3` diagnosable; at 2002 and 2023 none (the EXTERNAL rules say "may be accessed" in
the same rule). Planted cases in `test_reads.py` case 05d; the row and its refusal case are the registrar's.

The registrar then recorded the rows these two rules let close. Twelve derivations went on record under the new
`claude: 2026-11-16` signature, each as `docs/conformance/8/DRV-<rule-id>.md` with its record: six on the
`unpopulatable-antecedent` arm, four on `undefined-in-rule`, one `definitional`, one `indistinguishable-consequent`
(seven rows of the 1985 text and five of the 1989 amendment: `1989-RV-A.2.11.3-2`, `-3`, `1989-RV-A.2.22.4-3`,
`1989-RV-A.2.46.3-3` and `1989-RV-A.2.19.4-3`). The refuter's pass read fifteen argued rows: ten upheld, two
overturned (one of them with a witness the refuter wrote, which becomes a test) and three doubtful; all fifteen lines
stay in `tests/version-matrix/derivation-refutations/pb5822-1985-kept.jsonl`, the refused ones as the record that those
derivations were refused. One refusal witness closes `1985-GR-X.4.5.4-3`: `g85_bf31_07`, refused with COBOL1639 at
`--std 85`. The 1985 line moved from "102 GAP (77 inherited, no own-edition witness · 1 never adjudicated · 24 open)" to
"89 GAP (64 inherited, no own-edition witness · 1 never adjudicated · 24 open)".

**Third mechanism, kb/Work PB5831: a sixth derivation arm, `unfalsifiable-under-determination`.** For a rule whose
requirement holds for every behaviour a compiler could have, a defective one included, once a term the standard leaves
to the implementor is held at the meaning the conformance register documents (the term alone, never the behaviour a
register entry promises beside it: holding DOC-A.1-197's "adds no slack bytes" fixed would wrongly admit
§1985:VI.5.13.4 GR8b, which `g85_f09_02` witnesses). The schema arm is argued; its Names cell is one register key
(`DOC-A.1-<n>` or `D-<NAME>`); its check `register-key` refuses a key that is no row of the register with the new code
`determination-not-in-register`, in `Derivation._arm_refusals` and the C# twin. The register extract
(`tests/version-matrix/conformance-register.json`, `docchecks/register.py#build_extract`) gains `determinations`, the
section 3 and 3.1 keys, read by `Schema.determination_keys` and `ConformanceRegister.DeterminationKeys`. Six parity
cases (an own-verdict row on a section 7 key with an UPHELD refuter record, a section 3 key, a missing key, a Names cell
of the wrong shape, no refuter record, a chained row), five lineage self-test cases and the C# self-test's twins; no
real row is recorded on the arm. Also in this round: two audit reds the registrar found (a planted
derivation anchor typed as a literal in the drift test, now computed by `DerivationAnchor`; a self-test write without
`newline=`), and the comment of `g85_bf31_07.cob` no longer names a case that is not in the tree.

The second fold recorded five more 1985 rows and one register entry (all `claude: 2026-11-16`). By arm:
`unpopulatable-antecedent` for `1985-GR-VII.4.3.4-22`, `1985-GR-XIII.4.5.4-15` (one physical extent per file) and
`1985-GR-XIII.4.5.4-9` (on the new entry D-MEDIUM-85, section 3: every file at COBOL-85 is on a non-reel/unit medium);
`unfalsifiable-under-determination` (kb/Work PB5831, resting on DOC-A.1-197) for `1985-GR-VI.5.13.4-3` and `-4`. The
second refuter's tally on the rows it read: `1985-GR-XIII.4.5.4-9` upheld, `1985-GR-XIII.4.5.4-18` overturned (it is
witnessable, not a derivation), the two SYNCHRONIZED rows doubtful; a third reading on the sixth arm upheld both. Its
lines are in `pb5822-1985-kept-2.jsonl` and `-3.jsonl`. The 1985 line moved from "89 GAP (64 inherited, no own-edition
witness · 1 never adjudicated · 24 open)" to "84 GAP (59 inherited, no own-edition witness · 1 never adjudicated · 24 open)".

Merged with main after PB5754 and PB5699 landed (no conflict; their format rule screens a KEPT row's own test-refs
beside this branch's derivation predicate, and `bare_kept` still leaves out a derivation-closed row), with PB5819's
row recorded as waiting on PB5815 (`blocked_by`, and the refuter's witness program `g18rd2.cob` copied in full beside
the note): on the merged tree the 1985 line reads "37 GAP (12 inherited, no own-edition witness · 1 never adjudicated
· 24 open)", from main's 55. The second fold's two rows on the sixth arm put `DOC-A.1-197` in their §8 Names cells,
and the ANNEX A.1 audit's token count (`audit_annex_a1.evaluate`) counted them as prose: it now counts a `register-key`
arm's Names cell as a legitimate occurrence, as it learned the §8 row keys in PB386, with a planted pair of cases.
