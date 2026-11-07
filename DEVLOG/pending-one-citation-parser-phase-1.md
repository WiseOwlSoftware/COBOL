## One citation parser for every edition of the standard (PB4887 phase 1)

The owner asked on 2026-11-06 for one citation format across every edition of the COBOL standard, with the edition
mandatory: `§<edition>:<locator>[ <rule>]` (kb/Work PB4887). This branch lands phase 1 of 2. It adds the parser, moves
every reader onto it and writes the one-time rewriter, which is proven on a scratch copy and not run on the tree.

**The parser.** `scripts/spec/citation.py` is the format's specification and its only parser. It reads the edition
tokens 2023, 2014 and 2002 (clause numbers, annex clauses included), 1985 (the Roman section, then that section's own
paragraph: `VI.5.3.2`; a printed page `p.VI-20`; an appendix `B.2`) and the amendments 1989 and 1993 (their own
numbering, parsed only). It validates a citation against that edition's transcription. 2023 and 1985 are validated
today, and 2002 will be once `specs/ISO_COBOL_2002.md` exists. The 1985 region builder follows the transcription's
shapes: section headings named twice, unheaded paragraph lines, a folio line ending each printed page, and Amendment 1's
`SECTION A` closing the 1985 sections. A paragraph cited under the wrong section is refused, and the message names the
sections that have it (`§1985:VII.5.3.2` → `VI.5.3.2, X.5.3.2 exist`). `--self-test` plants every edition, every
locator shape and each rejection. `BARE_MEANS_2023 = True` reads the tree's older bare `§N.N` as 2023 until phase 2.

**The readers.** `cite.py --check` takes a whole citation (`"§2023:14.9.8.4 GR1"`). For an ISO rule it now also checks
the ordinal: text that sits under rule 1 fails a `GR2` citation. `--edition` belongs to `--find`, and combining it with
`--check` is refused. `audit_code_citations.py` lost `CITE` and `NOT_THE_STANDARD`. PHANTOM validates an edition-form
citation against its own edition. A malformed one (`§5000:`, `§2023:VI.5`) is the new CITATION kind. The BARE refusal
for phase 2 is written and self-tested, and stays switched off until then. The ordinal family's clause tokens no longer
read another edition's clause number as a 2023 one, and a designator that follows another edition's citation is not
filed under an earlier 2023 clause. `audit_doc_citations.py` embeds `citation.PATTERN` and checks a 1985 quotation
against the 1985 transcription. `fuzz_triage.py` lost `CLAUSE_IN_TEXT`. `work.py`, `where.py`, `phase_b_batch.py`,
`docchecks/vcr.py` and `r2_collect.py` also read through the parser. The `git grep` sweep finds no citation regex
outside `citation.py`. The `§` patterns that remain read a repository document's own sections (`r2_collect`'s design
references, `source_units`, `ci_bounds`, `work.py`'s site sections).

**Found on the way.** `docs/CONDITIONS_EXCEPTIONS_DESIGN.md` and `docs/DESIGN.md` cited seven transcription LINE
numbers as `§5000`, `§4970/§5018`, `§5002-5004`, `§5006`, `§24505` and `§4808`. The new grammar refused the first two
as malformed citations. All seven now cite their clauses (7.3.25.1, 7.3.25.4 GR1 to GR4, 14.6.13.1.1, 7.3.21), each
checked with `cite.py`. One VCR quotation was filed under E.3.3 but is the title of E.3. Reading annex clauses in the
bare form exposed it, and it now cites E.3. The bare form had read a clause out of the placeholder `§14.9.N`; the
scratch proof found this, and the bare form now refuses it.

**The rewriter.** `scripts/spec/rewrite_citations.py --dry-run` would rewrite 93,204 bare 2023 citations in 6,274
files. It rewrites exactly the set `citation.bare_standard` classifies as 2023's, which is the set the phase-2 BARE
refusal refuses. When another edition is named nearby, it decides the edition only by elimination. A number that no 1985
section has is 2023's (370 cases). A number 2023 does not have belongs to the named ISO edition (6 cases). The other
248 are listed for hand repair. The tooling's code is left alone, because its string literals are data its own logic
reads; a first scratch run that rewrote it broke two self-tests. On a scratch clone the rewriter ran `--apply`, then a
second `--apply` that changed nothing. `BARE_MEANS_2023` was then set to False. `audit_code_citations --check` reported
0 findings and `audit_doc_citations --check` 0 misfiled. The doc audits needed only `--write-register`, which phase 2
runs.

**Self-review.** Architecture: one parser, one pattern that other patterns embed, one classifier shared by the rewrite
and the refusal. Code: the rule check compares only the top-level ordinal that `rule_path` prints, and does not check a
1985 rule. Performance: the phantom scan skips a line without a `§`. Duplication: the ISO heading reader of `cite.py`
and of `vcr.py` is now `citation.iso_regions`.
