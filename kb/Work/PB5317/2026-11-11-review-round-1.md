## Review round 1: the restarted-list example, quoted citations, and the residue filed

<!-- audit-code-citations: names-phantoms (the answers name the wrong citations they repaired) -->

The branch review at 0ea0a1a7c0 recorded three findings (`E:/COBOL-coord/scratch/review-fix-cite-1985-ordinals.jsonl`).

**R1-1 (low).** `citation.py`'s `_rule` docstring and self-test justified reading `GRLm.n` in ISO editions with 7.2.3.4,
whose general rules are one unbroken list GR1-GR13. The example is now §2023:9.3.6 GRL2.1 (method invocation restarts its
list for the resolution steps; `cite.py --check` answers OK with the text of step 1).

**R1-3 (medium).** RESOLVE skipped every citation `_reported` exempts, and that function's third arm (a citation between
backticks or quotes is prose NAMING a spelling) is PHANTOM's: it hid the commonest citation form in docs/ and kb/Work, and
every citation in a Python or C# string literal. RESOLVE now keeps only the disclaimer and the repair-arrow exclusions
(`_repair_record`, which also reads an arrow after a closing backtick or quote); `resolve_self_test` plants a wrong
citation in backticks and in quotes (both now fire) and a quoted repair record (silent). The review's probe counted 1,644
newly read failures, but most of them were the baseline file's own entries: the baseline is a record of failures, not a
citer, so `resolve_population` no longer reads it (read as a citer it would list itself again at every rewrite). Read
honestly, the quoted citations add 41 entries net (`--write-baseline --grow`, the checker made stricter: 1,589 to 1,630).
Repaired in the files this branch writes: the deliberately wrong citations of the self-tests in `cite.py` and `citation.py`
carry the example marker; the module docstring of `audit_code_citations.py` says on each line that its examples are wrong;
READ's GR9 in its parse-gap comment was cited under RELEASE's clause (§2023:14.9.32.4; now §2023:14.9.30.4 GR9); a
returned-value rule in `extract_rule_catalog.py` was cited as a general rule (now §2023:15.30.3 RV2b); citation.py's parse
example `§2023:8.9 Table 12` is now §2023:14.5.1 Table 12, where the table is captioned. The example citations
`§1985:VI.5.3.2 GR3` and `§2002:14.8.4 SR2` in `docs/DOC_INDEX.md` and `CONTRIBUTING.md`, siblings of the CLAUDE.md example
this branch corrected, are now §1985:VI.5.3.4 GR3 and §2002:14.8.4.2 SR2.

**R1-2 (medium).** The refused citations PB5318 does not own are filed as kb/Work PB5333, with the fix direction. PB5318's
count is now its measured class, 1,514 (edition 2023, `no-rule-list`, the message naming a subclause or sibling clause).
Neither population is rewritten here.

**Merging origin/main** brought the 2002 rule catalog (kb/Work PB5256): `cite.resolve` now reads 2002 against it (LABEL
MODE is 2014's alone), the self-test's real-tree 2002 cases assert an unmarked verdict, and the page furniture follows the
extractor's per-year `furniture(year)`. It also brought kb/Work PB5312, whose correct `§1985:VII.2.12.3 SR1` RESOLVE read
against the SR2 quotation of the next sentence. That was the pairing reader's defect, with a sibling: a prefix pairing ran
past a citation that closes its own sentence (`(... SR1), and ... "quote"`, `audit_doc_citations._leaves_parenthesis`, now
no pairing in either audit) and past the next rule the prose names (`GR3 puts ... and GR4 b moves it "quote"`,
`names_another_rule`: the doc audit keeps that pairing for the clause, RESOLVE reads the citation without the quote). Each
shape has a self-test case that fails without its fix. Thirteen baseline entries were those false positives, among them
three of the 1985 eight (PB5286's §1985:XIV.3.3.4 GR2, PB5176's and a store record's §1985:VI.6.19.4 GR3, each right) and
the SetEmitter and StatementValidation entries the review cited. The baseline is 1,617: PB5318's 1,514 and PB5333's 103
(2023 92, 1985 7, 2002 4).
