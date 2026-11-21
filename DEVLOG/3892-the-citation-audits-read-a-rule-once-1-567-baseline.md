## Entry 3892 — 2026-11-21 01:58 PST — The citation audits read a rule once: 1,567 baseline citations repaired, a rule chain and a table row are read, the analyzer sees an inline int choice

Wave 20262121 group A took four notes that share one root, the citation tooling: PB5318 (two readers of one question),
PB5333 (the citations RESOLVE refuses that no rewrite owns), PB5935 (the edition-cite analyzer's hole) and PB5938 (the
table-row quotation the doc audit never paired).

**PB5318.** `audit_code_citations.py` decided a rule number twice: RESOLVE through `cite.resolve`, and the ordinal family
(RULE, SUBITEM, QUOTE-RULE, FORMAT, behind `_alias`) over the 2023 standard alone. The two disagreed on 1,514 citations,
every one a rule written under its statement's clause (`§2023:14.9.39 SR17` for `§2023:14.9.39.3 SR17`). The re-probe on
this tree measured 1,567 baseline entries (1,499 `no-rule-list`); RESOLVE printed the repair for 1,468 of them and a script
applied each in its file and line (420 files, 1,468 sites, nothing else touched; the DEVLOG, the store records and the
generated registry were left to their own tools). Then the four arms, `_alias`, `_another_clause_admits`, `quote_rules`,
`_quote_findings`, `_sub` and the dead sub-item groups of `RULE_CITE` went; `spec_ordinals` returns `(formats, owner)` and
FORMAT-RULE finds a block through `_block_owner` (a lookup under the statement's clause, never an alias). Deleting the arms
exposed what they had covered that RESOLVE did not: a rule CHAIN (`GR6/GR99`) was checked by RULE for every designator and by
RESOLVE for the first only. `citation.chain` now yields each later designator of the same kind, and RESOLVE reads them
(self-test legs in `citation.py` and `resolve_self_test`; the retired arms' legs moved to `resolve_self_test`, the
declaration list `self_test_arms.py` names them). The chain found six real defects the old arms had missed or accepted
(`§2023:13.18.38.3 SR25c/SR25d` cites a lettered item the transcription loses, `§2023:13.18.44.3 SR20` and `GR26` belonged to
other clauses, `§2023:8.5.1.8 GR7/GR8` is `§2023:13.18.38.4`). QUOTE-RULE read quotations of three words; RESOLVE reads
`cite.MIN_WORDS` (four): the one deliberate narrowing, recorded in the module docstring.

**PB5333.** 98 entries were left after the rewrite; each was read against the standard. Mechanical, with the suggestion
RESOLVE printed: 28 `no-format` (a format belongs to the statement's clause, `§2023:14.9.39 Format 9`), 21 `no-table`
(Table 19 is the OPEN statement's, `§2023:14.9.27.4`), and the argument and returned-value rules of the intrinsic functions
(`§2023:15.19.3 SR3` is `AR3`, `§2023:15.50.4 GR6` is `RV6`). A message string that carried a format before its rule moved
the format after it (`§2023:14.9.39.3 SR21, Format 9`), because DIAG-UNQUALIFIED asks a message to carry the subclause its
rule lives in. By judgment: `§2023:14.6.2.3.2 GR7` is the second list's `GRL2.7` (four sites); `§2023:8.3.3.6 Format 8`
for NULL is `§2023:8.4.3.1 Format 8` (predefined-address); `§2023:14.2.3 GR8` quoted "occupy" where the rule prints
"occupies", so the quotation was corrected and not the number; `§2023:12.3.7 Format 4` names a format the paragraph does
not have (it prints one), now `§2023:12.3.7.2`. The decision the note asked for first: the 1985 mixed block
`§1985:IV.4.2.2.2.3` numbers its syntax rules as the third list in the catalog (`SRL3.1`, `SRL3.2`), the catalog is right, and
the generator's `SR1`/`SR2` were wrong; the four construct rows were corrected and `ConstructRegistry.g.cs` regenerated. The three store records
(two 1985, one 2023) were restated through `record_verdicts` (notes only; GAP 0 to 0). The baseline holds no entry now.
A citation `§2002:9.1.12.1 GR2a` is written `§2002:9.1.12.1, item 2 a)`: the 2002 catalog carries that clause's numbered
paragraphs as no rule.

**What the rewrite broke and the self-review caught.** The mechanical rewrite also changed lines that were planted counter-
examples: `cite.py`'s own self-test (three `fails(...)` legs), `audit_annex_a1.py`'s control row, and two DOC-A.1 rows whose
clause is the identity `audit_annex_a1.py` compares to Annex A.1 (A.1 prints the statement's clause). The first two were
restored and marked `citation-audit: example`; the two rows keep the statement clause beside the subclause citation
(`A.1 cites 14.9.18.3 GR10 (printed as §2023:14.9.18.4 GR10 in this text)`). Widening `audit_annex_a1.corresponds` to a
parent clause was tried and reverted: it let the replayed DOC-A.1-209 misfiling pass.

**PB5935.** The analyzer's interpolation arm took a hole only through `HeadOf` (a derived member), so
`$"§{(edition.Year < 2002 ? 1985 : 2023)}:8.12"` and the switch-expression form escaped. The arm now reads an inline
conditional or switch expression in a hole as the ternary it is (its condition's reads), skips a string-armed one (it is
its own candidate), and unwraps a cast, a `!` and parentheses; `HeadsOf` reads both sides of a `??`. Six planted holes (inline
int, inline switch, cast, `??`, and the legal twins that print the compile edition and a non-edition condition) in
`TheAnalyzer_ReportsEachShape_AndEachAlias`; the four shapes failed before the change (no diagnostic).

**PB5938.** `citations()` read a quotation beside a citation only in a prefix or a postfix form; a table cell separator
ends neither. `_table_pairs` pairs a cell with ONE quotation and the next cell with ONE citation (a cell that quotes twice,
or cites twice, is ambiguous and left to RESOLVE), a whole-cell quotation keeps its capital opening, and a quote the prefix
or postfix form claimed stays theirs. DESIGN-listing.md went from 0 to 10 paired quotations, all correct; the corpus count
rose from 1,995 to 2,095 with 0 misfiled. `unread_docs` makes the blind spot a red: a doc with at least four table rows
that quote the standard beside a checkable citation, and no pairing at all, is UNREAD (self-test: planted doc fires, a doc
that pairs is silent). The postfix form also accepts a sub-item letter after the rule (`GR9 a:`), which had paired a
quotation with the citation before it.

Gate: see the report. LINUX GATE: not part of the per-change gate (PB4327).

