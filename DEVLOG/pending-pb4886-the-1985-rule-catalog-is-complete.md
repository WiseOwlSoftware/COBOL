## PB4886: the 1985 rule catalog is complete, committed and held by the gate

kb/Work PB4886's step 1. On main, `python scripts/spec/extract_rule_catalog.py --edition 1985 --check` exited 1 with
FINDINGS: 39 (re-probed on this tree: the same 39), so the 1985 audit's map pass had no denominator to run on. Now it
reports no finding and reads 2,057 rules from 622 blocks (SR 597, GR 1,105, AR 65, RV 80, FMT 210; Nucleus 323 level 1 and
184 level 2, Report Writer 296, Inter-Program Communication 38 and 60, Section IV 43 with no level). The catalog is written
to `docs/rearchitecture/spec-rule-catalog-1985.json`, and a new audit, RULE CATALOG 1985 (`scripts/gate.py` AUDITS and
CI's audits job), runs the check, so a later transcription repair that reopens a gap or leaves the committed catalog
stale turns red.

**The 13 unresolved headings.** The 1985 arm kept its own kind table, a subset of 2023's. It now imports
`extract_rule_catalog.KINDS`, the one table, which types "Editing Rules" GR and "Precedence Rules" SR in both editions
(§2023:5.3.3 and §2023:5.3.2 type them). VI.5.9.6, the precedence rules, is unnumbered prose and a chart, so it is a known
empty block, as 2023's 13.18.40.6 is. The other headings are typed by locator in the new `rule_catalog_1985.BLOCK_KINDS`,
each with its reason under §1985:IV.3.1/IV.3.2, and with a kind per rule where the rules differ: Formation and Evaluation
Rules (SR3-4, the rest GR), PAGE-COUNTER Rules (SR2, 3, 6) and LINE-COUNTER Rules (SR2-3). Standard Alignment Rules,
Condition-Name Rules and the four presentation rules tables are GR. VI.6.4 and XIII.3.10 are parent headings with no
rule, so they go in `EXCLUDED_BLOCKS`.

**The 18 duplicate ids** are the scan's repeat of folios X-11 and X-12 (PDF pages 527-528). The transcription keeps both
copies on purpose (its preamble, PB4850), so no text is removed. `citation.SCAN_REPEAT_85` reads the repeat's comment.
The catalog reads no rule from a repeated page, and in the sibling reader, `citation.regions_1985`, the repeat now opens
no second region.

**The ordinal break at XIII.3.19.4** came from the scan gap at XIII-54/55. The text after a gap is never the block
before it. Both the catalog and `regions_1985` now close the paragraph at `SCAN_GAP_85`, and `GAP_RESUMES` places PDF page
634's rules (5)-(7) as the TYPE clause's syntax rules (paragraph 3.20.3 of Section XIII, whose heading is on the missing
page). Before this change, `cite.py --check "§1985:XIII.3.19.4" "SOURCE clauses and associated USE statements must not
reference"` passed on a TYPE rule; now it is refused. A rule after a gap that no entry places is a new finding, and so is
an adjudication the transcription no longer has.

**The 7 amendment changes.** Four were fixed by the typing (VI.5.9.5 GR7, XIII.3.10.8 GR4, VI.5.15.6 GR6). For IV.4.2.1
Separators rules 4 and 5, the amendments call that paragraph's items rules, so it is admitted SR, as 2023's 8.3.5 is. The
two Section XVII changes named a rule only inside the new text they add, so a change's paragraph and rule are now read
from its directive, the text before `<br><br>`. The sweep also found that Addendum 1's PDF page 839 prints 25 change rows
without the table's pipes ("**IV-4** Paragraph 4.2.1, rule (4), ..."), and no reader had read them. `CHANGE_PROSE_ROW` now
reads them, and `CHANGE_RULE` reads "rule (4)". The catalog reads 239 change rows and amends 16 rules by X3.23a-1989 and
57 by X3.23b-1993.

**Not done: a content-keyed scan for 1985.** 2023's catalog also measures the numbered lists under headings that name no
rule block (PB29). The 1985 arm has no such scan, and about 83 paragraphs fit that shape (VI.6.4.2 The ON SIZE ERROR
Phrase among them). DESIGN-spec-conformance-review.md §10.1 now says so, and the wave report carries it as a lead.

Self-tests: `rule_catalog_1985.py --self-test` covers every new mechanism with planted cases, and `citation.py
--self-test` holds the repeat and the gap against the real file. `audit_batch.py plan --pass map` plans 2,057 items, so
every row's citation parses. The 2023 catalog's check is unchanged.
