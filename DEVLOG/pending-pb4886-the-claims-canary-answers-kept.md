## PB4886: the 1985 claims canary's right answers are kept, and a failed canary retries as the canary

The claims canary of the 1985 audit (batch `msgbatch_01WnTYzyAVaUgPU5Lj9RUy2t`) came back with three right answers, and
`scripts/spec/audit_batch.py` threw all three away. Each cited a Function paragraph as `§1985:VII.3.5.1 Function`. Many
1985 regions are a Function, a General Format or prose with no numbered rule, and the prompt asked for "the paragraph and
the rule", so the model wrote the heading word in the rule's place, and `citation.parse` refused it. `retry` then refused
to resubmit the three (PB4930): its todo held every never-submitted item, so it was never "only canary items".

**One citation shape.** An answer now names its 1985 `locator` and its `rule` (null where no numbered rule decides) as two
fields. `CITATION_FIELDS` is the wording the prompt uses, and `cited` reads the fields through `citation.parse` and
validates the locator against the transcription. A heading word, an edition inside the locator or a paragraph the text
lacks is a reply to retry. Only the claims pass's answer cites the 1985 text. The map, verify and drafts passes quote
their rule's catalog citation, and `plan --pass map` now parses each one. That check found a sibling: `citation.RULE`
refused the catalogs' own argument and returned-value citations (`§1989:A.2.5.3 AR1`, `§2023:15.10.3 AR1`).
`citation.RULE_KINDS` (SR, GR, AR, RV) is now the one list of rule kinds, and `cite.py --check` reads an AR or RV ordinal
through `NUMBERED_RULE`.

**Retry.** Until a canary is proven, `retry` resubmits that canary's own unanswered items as the canary. Otherwise it
refuses. The planted case ends a canary with two unparsed replies, retries exactly those two, keeps the full submit
refused while they run, and then proves the pass. The new claims cases fail on the old code (no `CITATION_FIELDS`, and a
`locator` answer was refused).

**The 1985 heads.** Appendix C's heading was an unheaded line, so the obsolete-element list folded into B.2.2 and Appendix
C had no region. The single-number paragraphs II.1, IV.4, A.1 (with A.1.1 and A.1.2, which sat in an indented code
block), A.2, A.3, C.1, C.3 and C.4 had no region either. The rendered pages (PDF 709 and 789) print them as headings, and
the transcription now heads them that way. Two contents lines on PDF page 15 had been written as headings, which gave
Sections X and XI a second region in the front matter. They are plain lines again. `citation.py --self-test` holds both
rules against the real file (every appendix the document names has a region and its first paragraph; no section opens a
region before Section I), and both fail on the old file. LEDGER-1985 records every edit: page 709's claims 1-4 are
APPLIED, and the `paragraph-heads(p709-1)` sweep covers the others. The lead that VII.2.12 (RERUN) lost its `2.12.1
Function` heading was refuted by the page: PDF 391 prints no such heading, so the function sentence is the paragraph's own
text, `§1985:VII.2.12`.

**The canary, collected.** Retry batch `msgbatch_01Q3YCEzM2sYhGXKSpJt1uKb` cost $0.01 (the pass has spent $0.03) and
returned three valid answers: VCR-7.1 CONFIRMED (§1985:VII.3.5.1), VCR-7.2 CONFIRMED (§1985:VI.6.15.1) and VCR-7.4
CONFIRMED (§1985:VII.3.6.1). Each citation passed `cite.py --check`. The full claims pass is left for the session.

No inventory row changed, and no code or diagnostic was used (COBOL6526-6528 were returned).
