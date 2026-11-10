## Entry 3399 — 2026-11-09 18:31 PST — PB5045: the 1985 transcription is kept current, both amendments applied in place and evident misprints corrected, each attributed

The owner ruled on 2026-11-09 that the markdown transcription is the most accurate version of the specification: it
corrects evident misprints, applies what later revisions correct or change, attributes every modification, and never
keeps incorrect text because the printed page carried it; and the document says so. Until now
`specs/ANSI_COBOL_1985.md` did the opposite: "an amendment is never merged into the text it amends", and a misprint
that the sense showed (PB5031's `index-name-1`) stayed as printed.

**Misprints.** The leads PB4984 left as printed were re-judged, each on its rendered page. Five are corrected (C31-C35):
REDEFINES syntax rule 6 prints `the data-name referenced by data-name-2` where its own first sentence says `data item`;
SET general rule 2 prints `index-name-1` for index-name-2's own table (PB5031: general rule 3a provides for the two
indexing different tables); and the REWRITE rule of all three I-O modules copies the WRITE rule's `associated output
file`, though a REWRITE file is open in the I-O mode. Six stay, because they read right (`multiplier` names the BY
operand). The MOVE category list without alphanumeric is Addendum 1's own correction (row VI-104 (T)). A sixth, found
on the way and raised as a review lead (R1-3), is corrected too (C338): page 107 (II-23) prints `may.call` in 6.4.1.2's
first item, a stray period the rendered page shows.

**Amendment 1, Addendum 1.** Every change row of its pages A-1 to A-24 (PDF 831-854) is applied where it belongs, in
its order, each read on its rendered page first: 141 Errata entries (C36-C176) for the table of contents, Section I's
module counts and diagram, the element summaries (the Intrinsic Function list, the 318 lines of I-54 to I-63
indented), the glossary, Section IV's function-names, classes, reference modification and identifier, the new 4.3.8.3
Function-Identifier with 4.3.8.3-4.3.8.5 renumbered behind it, the Section V and VI formats (subscripting with ALL,
SYMBOLIC CHARACTERS, the OCCURS box, the CD formats), the function-identifier syntax rules of the I-O statements, the
level-2 boxes the Addendum adds, and Appendix A to C. Three rows change only what the transcription does not draw; the
index rows give way to Amendment 2's index; Section A stays where Amendment 1 prints it, its one home (C176). The tool
that applies a batch (find, replace, flag, Errata row and entry) and the ledger and figure-verdict maintenance live in
the wave's scratch; the reconciliation ledger gains the added boxes, and every figure an amendment changed carries a
`drawn` verdict naming its Errata entry.

**The text says so.** The Preface, the About note and the Errata introduction now state the rule; specs/README.md and the
2002 About note follow; design 10.1 and 10.2 record the one-home decision (an amended rule is read from the body, with
the amended number; the amendment's change row only records `amended_by`), and 10.3 the misprints pass's correction rule.

**Amendment 2.** Every change row of pages B-1 to B-48 (PDF 913-960) is applied in place, in its order, each read on its
rendered page: 161 Errata entries (C177-C337). Among them: the Preface and the table of contents; SPECIAL-NAMES, PICTURE,
the class condition, IF, INSPECT (its general rules renumbered), PERFORM and SEARCH; the File Attribute Conflict
Condition paragraph 1.3.7 deleted from the three I-O modules (the LINAGE-COUNTER paragraph renumbered 1.3.7; C139's flag
stays to say the paragraph it corrected is gone); the OPEN, READ, REWRITE, USE and WRITE rules of the three I-O modules,
with the RECORD and sequential WRITE general rules renumbered; I-O status 22; the index-name, EXTERNAL and EXIT PROGRAM
rules; MERGE and SORT (Amendment 2 itself corrects `had been execution`); COPY and REPLACE; the Report Writer rows;
Section A's argument rules and MAX, MIN, ORD-MAX and ORD-MIN; and Appendix A to C. One misprint inside Amendment 2 is
corrected as a misprint (C336: its new undefined-list item 30 cites MERGE `general rule 17c`, which is 7c). Two
insertions that run over many pages and carry no rule are held once where Amendment 2 prints them, as Section A is: its
paragraph 2.2B of Appendix B (seventeen pages, C332) and its replacement index (C337), whose row also deletes the base
index's entries (C19's flag stays, as C139's does). Design 10.2 records both.

**The catalog follows the text.** `rule_catalog_1985.resolve_change`'s ADD path now finds the body's row at the number the
amended text gives it and records `amended_by` there; it adds no row, so the catalog has no `1993-*` row and no `§1993:p.`
citation, and `Catalog.shared_citations` loses the page-cited exemption (the self-test plants the added rules in the body
and a row that adds a rule the body lacks, which is reported). The regenerated catalog (2274 rules) moved: the
Reference Modification rules to IV.4.3.8.4 behind Amendment 1's Function-Identifier, the INSPECT, RECORD and WRITE
general rules, Amendment 1's and 2's added rules into the body, the three deleted 1.3.7 paragraphs out. The scan
manifest follows (the deleted paragraphs out, four extended appendix lists recounted, Amendment 1's new list in IV.4.2.2.1
admitted as syntax rules). The store moved with it through one `record_verdicts.py --edition 1985` batch, through the
new WITHDRAW record (a record the view refuses because its rule is gone or its id now names another rule;
`record_verdicts` had no way to answer the view's own "move or withdraw" refusal): 31 change records restated on the ids
that now name their rules, each only where the rule's text there is the text the record was decided against, and 115
withdrawn. Of those, 97 are records whose rule an amendment or a misprint correction changed in place: each was decided
against the printed text, so it is withdrawn and its row left for re-adjudication against the amended text, never
carried (kb/Work PB698's drift refusal). The first batch of this branch had carried them, stamping the amended text's
digest on a record decided against the old one; branch review round 1 refused that (R1-2), and the store was rewound to
main's and moved again. Two of the 97 hold a `constructs.json` edition gate, which EDITION GATES refuses to rest on an
unclassified rule, so they were decided again against their amended text in the same change: the CD entry formats
(XIV.2.2.2, Addendum 1's comma deletion) stay REMOVED, and the CLOSE table (VII.4.2.4 GR3, Amendment 2's file position
indicator in F) stays CHANGED, its reason no longer counting the position indicator as a 2023 difference. The C338
correction above re-decided the one record on its rule the same way. The inventory view is green; 1985 GAP 1052 (main)
to 1081, and the other 95 rows are among the view's 132 unclassified 1985 rules, waiting for re-adjudication.

**The amendment rows the catalog could not see** (review R1-1). Page 931 printed IX-30, IX-30 and IX-31 as dash-joined
prose, which neither change-row shape reads, so `parse` swallowed them as IX-27's new text and indexed READ GR10 and GR11
recorded no amendment while their sequential and relative siblings did. `rule_catalog_1985.CHANGE_UNREAD` now makes a line
that opens with a page cell in neither row shape a finding, never text. The sweep it ran found twelve more: Amendment 1's
IV-21 to IV-23 rows on pages 841 to 843 and Amendment 2's XVII-80 on page 942, printed without their page cell's bold.
Each was read on its rendered page; page 931 is one table, the others the bold row form pages 839 and 844 already use
(LEDGER-1985 claim p931-0 APPLIED, twelve `change-row-shape` sweeps). Reading them exposed two more gaps in
`resolve_change`: a lowercase "New paragraph 4.3.8.4.4" named no paragraph, and a change to a general format recorded
nothing, as no change ever had. `CHANGE_PARAGRAPH` reads both cases, and `record_format_change` records a change to a
format on its FMT row (a change to a format the catalog does not harvest, Section V's skeleton or a paragraph 2.1, names
no row). Ten rows gained `amended_by`: FMT IV.4.3.8.2.2, IV.4.3.8.4.2, VI.6.16.2 and XIV.2.2.2, SR IV.4.3.8.2.3-7 and
IV.4.3.8.4.3-5, GR IV.4.3.8.4.4-1 and -3, and GR IX.4.5.4-10 and -11. The self-test plants each case.

**Left as found.** `sweep_figures.py --edition 1985 --check` fails on main at page 200's figure verdict (no audit runs it);
it is kb/Work PB5052, filed on main from the previous wave's report.

**Gate.** `=== BUILD-LOCAL GATE: GREEN — Conformance 22,767/22,767 · Unit 40,620/40,620 · Characterization 37/37 cases ran (skipped 0) (run 000002-20261110T001215Z) ===`. After branch review round 1 (R1-1, R1-2 and lead R1-3 above): `=== BUILD-LOCAL GATE: GREEN — Conformance 22,783/22,783 · Unit 40,620/40,620 · Characterization 37/37 cases ran (skipped 0) (run 000003-20261110T020921Z) ===`; the two runs before it were RED on EDITION GATES (two withdrawn records under a constructs.json gate, re-decided) and on SELF-TESTS (CHANGE_FORMAT spelled the general-format noun itself, now spec_headings.GENERAL_FORMAT_NOUN).

