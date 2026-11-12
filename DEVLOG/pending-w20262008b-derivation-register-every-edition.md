## The derivation register takes rows of every edition; two review findings of the data-binder branch

Wave 20262008 group B finished the w20262006 b branch (PB5165 and its data-binder siblings) and its review, and built
kb/Work PB5374.

**Review R1-1 (blocking): an inherited GLOBAL constant against a farther GLOBAL data item.** The finisher's fix to
`DataBinder.ConstantOfIdentifierPosition` counted only depth-0 data items for a constant the element declares itself
and every data item for a constant inherited from a container. Re-probed (`n3.cob`: OUTERP `01 KK PIC 9 VALUE 1
GLOBAL`, MIDP `01 KK CONSTANT IS GLOBAL AS 5`, INNERP `DISPLAY KK`): COBOL1639. §2023:8.4.6.2.1 GR3b.1 makes the nearer
declaration the referenced item, so the count is now the data items at the constant's own tier (0, or
`InheritedDepthOf`); a nearer data item has already dropped the constant (`InheritStep.DropShadowed`). After: `5`, and a
container declaring both a GLOBAL constant and a GLOBAL data item KK still draws COBOL1639. Goldens
`8.4.2.2-Qualification/pb5301_global_constant_nearer_than_farther_global_data` and
`pb5301-global-constant-and-data-same-container`.

**Review R1-2: the citation of the 1985 condition-name size refusal.** COBOL1740 on `88 XXC VALUE "ABC"` under `PIC XX`
at `--std 85` cited §2023:13.18.63.3 SR4, which does not reach a condition-name. `ValueSubject.CharacterSizeRule` now
names the rule per subject kind (§1985:VI.5.15.3 SR3 for a condition-name), read by both character-size arms of
`ValidateValueCategory`; pinned in `ConditionNameValueSizeTests` and the pb5190 diagnostic expectation.

**PB5374: the derivation register served the 2023 inventory only.** A fifth arm, `undefined-in-rule` (mechanical: the
row's own catalog text declares the result undefined, the quotation checked inside it; refused where Annex A.2 lists
the rule); `signers` with the deciding session's `claude: <date>` keys mapped to a decision note, and an argued arm so
signed standing only on an UPHELD refuter record named in §8's new `Refuted` column; every check reads the row's own
edition's catalog (`Schema.catalog`); the audit, the drift test and the population guard read every held edition and
refuse a §8 row no row claims. Both engines and the parity fixture (28 cases) changed together. No row is recorded:
PB5373's adjudication records the 1985 class 1 and 2 rows.
