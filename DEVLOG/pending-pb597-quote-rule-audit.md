## PB597: the citation audit compares a quotation to the rule it is in, and the last wrong GR numbers are corrected

**The defect.** `cite.py --check`, `audit_doc_citations.py` and `audit_code_citations.py` all stop at the clause. A
comment reading `§14.9.25.4 GR8 — "the absolute value"` passed all three: the clause is right, the words are in it,
and nothing compared the number `8` to the rule the words are printed in (GR6 d) 2. b.; GR8 is the dynamic-length
rule). kb/Work R10 had swept that one wrong GR and recorded it done; it covered 5 of 14 sites while the audits read
zero.

**Re-probe (tree 86fd76dbd).** Of the note's eleven sites, four had been repaired since it was written (the SEARCH
"GR8 varied item" comment went in PB388's rewrite, commit 044136739; the six `SR-14.9.37.3-6` sub-clause
parentheticals are right in the inventory row and in `l1_search_when_condition_forms.cob`). The live ones were
corrected to the printed rule: the unsigned-magnitude sentence is `§14.9.25.4 GR6 d) 2. b.` in `CobolNum.Image.cs`,
`BinaryCapacityTests.cs` (2), `RecordImageCodecTests.cs` (2), `docs/DATA_MODEL_DESIGN.md` (2) and `docs/DESIGN.md`;
the serial SEARCH no-match in `ec_range_search.cob` is GR4 (GR6 is the Format 2 rule that applies only when GR5 is
violated), and SEARCH ALL's no-match with a sequenced table is the EC's own definition in §14.6.13.1.6;
`SetEmitter`'s "SEARCH's GR8b varied index" and `tests/nist/corpus.tsv` NC236A's "GR8b" are GR3 b)/c) and GR3 c) 2.
(SEARCH has no GR8b: GR8 is one sentence). Every citation was run through `cite.py --check`.

**The fix, at the root.** `audit_code_citations.py` gains QUOTE-RULE. A rule designator (`GR8`, `SR 3`, `rule 6`, a
chain `GR4/GR6/GR9`, a range `GR12-13`) beside a quotation is resolved: the quotation is looked up in the cited clause
(and its `.3`/`.4` child, since the repository cites `§14.9.25` for `§14.9.25.4`) with `cite.py`'s own `norm` and
`rule_path`, the first label of the path is the top-level rule, and a number the quotation is not under is the
finding. Only the top-level number is compared, so `6) 1.` and `6) d) 2. b.` (the imprecise nested numbering the note
warned about) are one rule, and `GR8` against `6)` is red.

**What the self-review found, and the cost of precision.** The first draft paired a quotation with the nearest chain
and accused 63 sites; five were real. The rest were prose that names several rules around one phrase, a chain that
opens the next sentence, an elided quotation, a quotation filed under a clause cited just before it, the 2002
post-ballot text's different numbering, and one real parsing trap: `FORMATS 1 AND 2` is a banner that heads GR4 and
`rule_path` files it under GR3. Each is now a silent calibration in `ORDINAL_SELF_TEST`, taken from the tree, next to
the two firing shapes (the quote before its parenthetical cite, and after the designator) and the depth case. The
tree is at zero. The audit cannot see a citation with no quotation (`§14.9.25 GR8 — an unsigned receiver stores the
magnitude` carried none); those were fixed by hand. The run also found a ninth shape no note listed: `kb/Work/PB206.md`
attributed SR10's bit-group sentence to SR5.

**Rows.** The three rows PB597 claimed (`GR-14.9.25.4-6`, `GR-14.9.37.4-6`, `SR-14.9.37.3-6`) were CONFORMS and OK
already; the defect was a citation number beside them, which no verdict reads, so the note closes none
(`closes_rows: []` with the reason). No compiler behaviour changed; no golden's expected output changed.
