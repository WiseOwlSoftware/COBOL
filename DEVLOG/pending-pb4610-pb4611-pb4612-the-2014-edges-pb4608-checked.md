## PB4610, PB4611, PB4612: three 2014 edges PB4608 moved to 2002 (the numeric-edited VALUE size rule, ENTRY-CONVENTION, USAGE POINTER TO type-name)

PB4608 read the 2014 FCD 1.0 and the 2002 draft against the construct rows whose 2014 edge rested on Annex E's silence. Three
rows moved, and this change fixes all three (one root: the edition edge in `DataBinder` / `VersionConformancePass` /
`constructs.json`).

**PB4610, the oversize VALUE literal on a numeric-edited item.** Re-probe on main's build: `01 X PIC ZZ9 VALUE "ABCDEF".`
compiled clean at `--std 2002` and 2014 and drew COBOL1570 at 2023 only. The rule is §13.18.63.3 SR4's size sentence, which
stands unchanged in 1989:2002 (`t2002.txt:19799-19806`, with SR8: editing characters count toward the size), the 2014 FCD
(`t2014.txt:23752-23754`) and 2023; Annex E.2 item 27 describes the content of a literal that fits, not its length. The check
sat in `ScreenValueLiteral` behind a hard-coded `DialectLevel >= 2023` and a `Removed` construct row (a second copy of the
rule with an edition axis it does not have). It is now one arm of the size rule in `ValidateValueCategory`, beside the
alphanumeric arm (COBOL1740), measured against the same `ValueSubject` size, an Error at every edition. The row
`value-numeric-edited-oversize-removed-2023` is deleted (no alias), and so are the `--permissive` downgrade and the test
beside it. 1985 is decided explicitly: refused, because no 1985 text is held, the 2002 Annex F.1 list names no such change,
and the alphanumeric arm of the sentence was already refused at 85. Goldens: `pb4610_value_size_numeric_edited` (85 to 2023;
exact-size literals on `ZZ9` and `$ZZ9.99`, the latter proving the editing characters count) and
`pb4610-value-oversize-numeric-edited` (refused at all four editions). SR-13.18.63.3-4 re-recorded; its note had put the
numeric-edited size change at 2023.

**PB4611, ENTRY-CONVENTION IS name at 2002.** Re-probe: `ENTRY-CONVENTION IS STDCALL` at `--std 2002` drew COBOL2385 and a
false COBOL0900 "requires COBOL-2014". The clause is in 1989:2002 (11.8.5, `t2002.txt:10979-10997`); only the COBOL
alternative is the 2014 FCD's (`t2014.txt:13144-13146`, GR2 at 13166). `VisitEntryConventionClause` now asks the row of the
`entryConventionName` rule's COBOL alternative alone. Row `options-entry-convention-2014` keeps its id and edge, re-scoped;
VCR row 7.50 records the 2002 clause. After the fix STDCALL at 2002/2014/2023 draws COBOL2385 alone and `IS COBOL` draws
COBOL0900 below 2014. Goldens: `pb4611-entry-convention-cobol-below-2014` (refused at 85 and 2002),
`pb232-entry-convention-name` now refuses at 2002 as well; `EntryConventionGateTests` pins the absence of the false 0900
(a refusal test cannot). The six ENTRY-CONVENTION inventory rows (FMT-11.9.7.2, SR-11.9.7.3-1, GR-11.9.7.4-1 to -4) lost
their "conservative 2014" sentence and, where the 2002 text has the rule, gained 2002 in `editions`.

**PB4612, USAGE POINTER TO type-name.** The row `usage-pointer-to-type-2014` was `pending`, unwired (no caller of
`Constructs.UsagePointerToType2014`) and wrong: the construct is in 1989:2002 (`t2002.txt:19354`, 19422, 19574). Decision:
delete it rather than rename it `-2002`, since the TYPEDEF, TYPE and BASED rows already gate the form below 2002 (probed:
COBOL0900 at `--std 85` on `pb153_restricted_pointer`) and a second gate would repeat them. The golden
`pb153_restricted_pointer` now declares `accept: [2002, 2014, 2023]` (it compiles and runs the same output at all three).
`DATA_MODEL_DESIGN.md` and `ISO2023_CONFORMANCE_PLAN.md` said the feature was deferred; it has been live since PB153.

**Self-review.** Architecture: the size rule is one place, one `ValueSubject` size, no edition test. Full code: the new arm
cannot fire for a condition-name subject (no size) nor a group (category Group), as the old block could not; the COBOL
alternative test reads the parse tree rule, not text. Performance: no new allocation on a hot path (the arm is guarded by
the category and class first). Duplication: COBOL1570 and COBOL1740 stay two codes for the two arms of one sentence, as the
brief asked for COBOL1570. Sibling sweep (`grep "DialectLevel [<>=]+ *20|85"` over `Binding/`): the remaining direct edition
reads are the documented version-conditioned structural rules (CONSTANT RECORD with EXTERNAL, SYNCHRONIZED on a group, SIGN
clause), none of them a second copy of a construct row's rule; they were not re-read against the 2014 FCD here.
