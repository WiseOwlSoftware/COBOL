## PB4889: an unnamed report item may begin with NATIONAL; the report entry's name slot asks the data entry's predicate

**Mechanism.** `reportGroupEntry` (`levelNumber dataName? reportGroupClause* DOT`) offered its optional name to a
reservation-gated word (through `dataName`'s `reservedGatedWord`) with no predicate, so `10 NATIONAL COLUMN 1 PIC N(3)
VALUE N"ABC".` parsed both as an item named NATIONAL and as an unnamed USAGE NATIONAL item, ANTLR took the name, and the
funnel refused legal source with COBOL0901 at 2002, 2014 and 2023. §2023:13.15.2 prints `level-number [ entry-name-clause ]`
and `[ USAGE IS ] { DISPLAY | NATIONAL }`, and §2023:8.3.2.1 1) keeps a reserved word out of the name, so the clause
reading is the only one. The data description entry solved the same question in kb/Work PB845 with `entryNameHere()`,
whose speculative parse was hard-wired to `dataDescriptionBody`.

**Re-probe.** The reporter's `p4.cob` on a fresh build at dec152346c: COBOL0901 at 2002, 2014 and 2023; at 85 the name reading
stands (NATIONAL is a user word there) and COBOL0900 refuses the national item. After the fix: 2002 to 2023 report only the
file's own COBOL2199 (its absolute LINE 1 in an unpaged report); 85 is unchanged.

**Fix shape.** One predicate, two entries: `entryNameHere(int bodyRule)` parses the rule that reads the rest of the entry, and
the report entry's clause list became its own rule, `reportGroupBody : reportGroupClause*` (the screen entry's
`screenDescriptionBody` shape), because the speculative parse needs a rule. The 28 binder reads of the clauses go through
`reportGroupBody()`. The probe cache keys on the rule as well as the token. `ReadingParsesFollowDriftTests` now reads
every `entryNameHere(RULE_…)` the grammar writes and holds each rule to the period-alone FOLLOW set, so the next entry that
asks the predicate needs no edit to the test; `EditionReadDriftTests` records the new word-status site and
`PointerUsageOperandDriftTests`' report chain gains the body rule. DESIGN-frontend-grammar.md's entry-name paragraph says so.

**Sibling sweep.** Every rule beginning with `levelNumber`: the data entry had the predicate; the constant entry's body begins
with CONSTANT, so no clause reading competes with its name; the screen entry's `screenName` admits no reservation-gated word.
Inside the report entry every gated clause-leading word is covered by construction.

**Goldens.** `13.15-Report-group-description/pb4889_report_unnamed_national_item` (accept 2002, 2014, 2023: an elementary item
and a group entry whose first clause is NATIONAL, and `USAGE IS NATIONAL` in full; prints `[ABC D]`, `[PQ]`, `[MN]`) and
`pb4889-report-unnamed-national-at-85` (COBOL0900 at 85). No inventory row moved: FMT-13.15.2, SR-13.15.3-2 and
SR-13.18.60.3-7 were already CONFORMS.

**Self-review.** Architecture: one predicate, the body rule a parameter rather than a second copy. Performance: the probe runs
only for a gated word the edition reserves at an entry's head, as before. Duplication: none added.

**Gate.** `build-local -Priority BelowNormal` at the branch head, the whole Unit, Conformance and Characterization populations; its verdict line is in the wave report (w20261905e-PB4889).
