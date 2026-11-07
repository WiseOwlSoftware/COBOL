## The citations audit reads the tooling, string literals included; one example marker; the screen reads the line above (PB4911)

kb/Work PB4887 phase 2 rewrote the tree's citations into the edition form and made a bare one a refusal, but its
rewriter left the tooling's string literals by design and the hand pass missed many. `gen_ledger.py` went on
publishing clause 4.2.16 of the standard after a bare section sign on the conformance ledger, and the citations audit,
which read no script, stayed green.

**Re-probe.** The sweep of PB4911, re-run with each file's own-section set through the audit's own PHANTOM and BARE
readers on 593f5188f9: 330 findings in 66 of the 387 tooling text files (300 BARE, 21 PHANTOM, 9 CITATION).

**The mechanism, one root.** `citation_corpus.tooling_files()` brings the text files under `scripts/`, `tools/`,
`.github/` and `.claude/` into PHANTOM, CITATION and BARE (`audit_code_citations.file_findings`, the per-file scan the
audit and its self-test share). The checks that need a comment convention or a quote stay where they were. The one
exemption is `citation.EXAMPLE_MARKER`, the text `citation-audit: example` on the line itself, defined in
`citation.py`'s docstring: a line that shows the refused form on purpose carries it, and nothing else is exempt.
`citation.names_another_text` now reads the line above when only a comment leader (a Markdown quote's `>` included), a
string literal's prefix and quote, punctuation or the earlier members of a list of sections stand before the number,
so a document named at the end of one line owns the section number that opens the next (lead 3a of PB4887's phase-2
branch). The screen also knows an interpolated document name (`{BINDER_DESIGN}`) and the Unicode Standard.

**The decisions.** Each finding was decided by reading its line: 157 lines became the edition form; about 45 document
sections name their document (DESIGN-architecture-review, DESIGN-test-build-ci.md, CONFORMANCE.md, the
`kb/Work/PB4323.md` sections); about 55 lines carry the marker (the citation module's and the audits' own examples,
planted self-test data, the R2 collector's planted design references, test records whose bytes a test slices).

**Sibling sweep: citations the tooling composes.** A format hole after the section sign is invisible to every audit.
`gen_ledger.py`'s burn-down rows printed each clause bare on the published page and now render through
`citation.render`; `file_census_notes.py` wrote a bare Spec line into the kb/Work notes it files; the vault titles of
`gen_conformance_notes.py`, the messages of `r2_collect.py`, `where.py`, `extract_rule_catalog.py` and the 2023-only
checks of `audit_code_citations.py` print the edition form. The rendered ledger page carries only the edition form;
the operator publishes main's page after the landing.

**The design-doc candidates (lead 3b).** The heading-word mismatch filter over phase 2's docs/ diff finds 634
candidates in 56 files. Ranked first, the 49 whose number is a term entry of clause 3 (no heading words, the sign of a
document's own section 3.N): 22 were a document's section and name it again (13 in CRITIQUE-encapsulation, all
DESIGN-binder-bound-tree), 27 are genuine term citations. Five more in the next ranks were a document's section
(FILES_DESIGN's `DESIGN.md` 14.4, SURVEY-runtime-value's own items). 580 remain undecided, and they are not all genuine.

**Left for the 1985 wave.** Ten bare citations in `figure_extract.py` and `figure_geometry.py`, which the in-flight
wave 20261893 owns, are refused with no exemption; its successor fixes them before it lands (PB4911 lists them).

**Self-tests.** `citation.py --self-test` plants the line-above screen (a comment, a docstring, a Markdown quote, a
concatenated f-string, a section list, and the negative where words stand before the number) and the marker;
`audit_code_citations.py --self-test` plants a bare 4.2.16 inside a script's string literal (BARE), its marked twin
(silent) and the four trees' reach. Every changed script's self-test passes.
