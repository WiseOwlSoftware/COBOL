## The fix, and the sweep of every citation the repository holds

Branch `fix-cite-1985-ordinals` (2026-11-11). The defect was wider than the 1985 rule number, and both of the brief's holes
were real in every edition: the 2023 arm compared only the top-level ordinal through `rule_path` (never the kind, the
sub-item or a restarted list, and one line at a time), 2002 and 2014 were checked the same loose way, and a quotation of one
character ("x") was enough in all of them.

**What `cite.py --check` now requires (`cite.resolve`, the one checker).** The edition's transcription is held; the locator
names a region of it; the quotation has at least 4 real words (two characters or more, one a letter, after `norm`); with a
rule designator, the clause has a rule list of that kind, the list has the ordinal (`GRLm.n` names a restarted list), the
rule has the sub-item path (`GR6c`, `GR6d.2`, `GR3a1`), `Format n` is printed by the clause or its general-format
subclause, `Table n` is captioned in or under the clause; and the quotation lies inside the sub-item, rule, format, table or
clause cited. A failure prints `FAIL [<kind>]` and, where found, the right citation: `§1985:XI.4.4.4 GR10` with rule 7's
text answers `text-not-in-rule ... under GR7, not GR10`; `GR99` answers `no-ordinal ... GR1-GR14 (14 in all)`;
`§2023:14.9.8 GR1` answers `no-rule-list ... cite §2023:14.9.8.4 GR1`. A form it does not read fails as
`designator-not-understood`, never on the clause alone.

**Where a rule starts and ends.** For an edition whose inventory schema names a rule catalog (2023, 1985 with its
amendments), each catalog row is anchored to the transcription line its printed ordinal and first words open, and runs to
the next row of its clause: the catalog's boundaries, the extractor's decision (every row of both catalogs anchors; the
self-test holds that as a drift test). 2002 and 2014 have no catalog yet (PB5256 builds 2002's): they run in LABEL MODE,
rule blocks being the clauses the catalog's own kind table types by heading and rules running between column-0 rule labels,
every verdict marked `[LABEL MODE ...]`. Sub-items are read inside the rule from the printed labels by style level (ISO
`6)` `d)` `2.` `b.`, 1985 `(6)` `a.` `1)`), because the transcription flattens some nested lists onto column 0
(§2023:14.9.39.4 rule 2, §2023:14.9.23.4 rule 7).

**Decisions beyond the brief.** (1) `GRLm.n` is read in every edition (citation.py refused it outside 1985 because the old
checker read only the ordinal; the 2023 catalog restarts lists too, and the checker's own suggestion could not otherwise be
written). (2) A quotation is paired with its citation by `audit_doc_citations.citations`, the one pairing reader; a paired
quotation found nowhere in the standard (a paraphrase, a label) leaves the citation's structure to be checked alone, and
one misfiled to another clause stays the doc audit's MISFILED. (3) The doc audit no longer rules on a quotation of fewer
than 4 real words (8 such pairings had read as MISFILED under the new rule; none was a misfiling). (4) The baseline matches
an entry on file, citation and kind, not line, so an edit above a listed citation does not turn the gate red.

**The sweep** (`python scripts/spec/audit_code_citations.py --sweep`, the RESOLVE check over its gate population plus the
DEVLOG entries of the last 31 days): 99,616 edition-form citations checked (2023 94,936; 1985 4,156; 2002 351; 1989
158; 2014 9; 1993 6), 41,936 of them with a rule designator (1985: 2,843), 1,489 with a paired quotation (1985: 134).
**1,590 fail**, at 547 distinct citations in 427 files; 1,552 carry the right citation.

- by edition: 2023 1,578 · 1985 8 · 2002 4 · 1989, 1993, 2014 0
- by kind: no-rule-list 1,509 (1,484 of them a rule block's clause written as its statement's: PB5318) · no-format 28 ·
  no-table 21 · text-not-in-rule 19 · no-ordinal 10 · no-sub-item 3
- by directory: src 676 · docs 384 · kb 277 · tests 216 · conformance-suite 32 · scripts 4 · DEVLOG 1

The 1985 failures: `§1985:XIV.3.3.4 GR2` (PB5286; the text is GR3), `§1985:VI.6.19.4 GR3` (PB5176 and the store record of
1985-SR-IV.4.2.2.2.3-L3.2; the text is GR4b.3), `§1985:IV.4.2.2.2.3 SR1`/`SR2` (ConstructRegistry.g.cs: the catalog numbers
that mixed block's syntax rules as its third list, `SRL3.1`, so either the citations or the catalog's list numbering of a
mixed block is to be decided), `§1985:II.6.4.1.3 SR2`/`SR3` (two store records; the paragraph has no syntax rules),
`§1985:VI.5.3.2 GR3` (PB4887's title; VI.5.3.2 is a general format). The list is
`E:/COBOL-coord/scratch/cite-sweep/failures.json`; none is corrected here except the example citations in the files this
branch writes (CLAUDE.md, the spec-lookup skill, citation.py: `§1985:VI.5.3.2 GR3` named a rule that paragraph does not
have; now `§1985:VI.5.3.4 GR3`).

**What the sweep cannot see.** A 1985 rule number that names an EXISTING rule and carries no quotation beside it passes
every structural check: 2,709 of the 2,843 1985 rule citations are of that shape. Their ordinal is checkable only against
the text they claim, so the code-read and held-row notes should quote what they cite (four words or more).

**The baseline is debt.** `scripts/spec/data/cite-resolve-baseline.json` lists the 1,589 failing citations of the gate's
population (the sweep's 1,590 less the DEVLOG entry, which the gate does not read). The CITATIONS audit allows exactly those,
and is RED on a new failure and on a listed one that no longer fails; `--write-baseline` refuses to add an entry
(`--grow` is for a change that makes the checker stricter, named in its note). Probed at this branch: a planted
`§1985:XI.4.4.4 GR99` in a doc is `[RESOLVE]` RED, exit 1; a baseline entry that no longer fails is `[RESOLVE-STALE]` RED,
exit 1. When PB5256 lands the 2002 catalog, 2002 leaves LABEL MODE and its 4 entries are re-decided.

Filed from this work: PB5318 (the ordinal family duplicates RESOLVE and disagrees with it on the 1,484 statement-clause
citations), PB5319 (the catalog's FMT `formats` count and 4,000-character cut).
