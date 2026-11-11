## Entry 3481 — 2026-11-11 11:07 PST — PB4996: the 56 held 1985 rows adjudicated and refuted; 26 CONFORMS recorded (1985 GAP 478 to 452), three decisions, PB5307-PB5317 filed

Three Opus adjudicators decided the 56 rows held across `held.json` through `held-5.json`, and an independent refuter attacked every closing verdict. Set 1 has 17 rows: 9 CONFORMS, 5 PARTIAL, 2 DIVERGES and 1 NEEDS-OWNER-DECISION. Its refuter upheld the 9 CONFORMS and both spot-checks. Set 2 has 20 rows: 4 CONFORMS, 11 PARTIAL and 5 DIVERGES. Its refuter upheld 3 CONFORMS, found 1 DOUBTFUL, and upheld the three shared readings. Set 3 has 19 rows: 14 CONFORMS and 5 PARTIAL, and its refuter upheld all 14.

`coderead-85/held_batch.py` recorded the 26 upheld CONFORMS (9 + 3 + 14) with the adjudicators' code-locations. For these rows the adjudication supersedes the first read. The 1985 GAP went from 478 (records-85d) to 452. The ENTER row `1985-SR-VI.6.12.3-3` stays held in `held-6.json` until the ENTER fix lands.

I recorded three decisions:
- PB5304: PB191's true-offset choice covers the 1989 CURRENT-DATE row. Its row stays open; the implied verdict is DOCUMENTED-NON-SUPPORT.
- PB5305: a duplicated cd-name is one defect, on the uniqueness rule `1985-SR-IV.4.3.8.1-1`.
- PB5306: a destination named twice in one SEND gets the message once per occurrence.

The 28 non-conforming rows were rerun on a build of origin/main d9dde0e7ba.
- 14 are already claimed by open notes, PB5208 to PB5216, PB5168, PB5187 and PB5176.
- 3 are added by addendum to PB5163, PB5238 and PB5288.
- 11 are in new notes PB5308 to PB5315.

Two more defects came from the decisions and the refuters' sibling charges: the duplicate cd-name (PB5307) and the level-01 elementary report entry that landed PB5079 left (PB5316). PB5317 is a tooling defect: `cite.py --check` does not validate a 1985 rule number, so "§1985:XI.4.4.4 GR10", and even "GR99", pass on a rule-7 quotation. For this batch I compared each 1985 rule number by hand. PB5108 gets its SUBTRACT row by addendum, because `inventory_rows` refuses a 1985 id (PB5094).

docs/CONFORMANCE.md changes:
- PB191's entry now holds for 1989.
- The label convention rests on implementor-defined items 57, 58, 60 and 61, not on the hardware exemption.
- The PADDING CHARACTER row names GR5.
- DOC-A.1-147 and DOC-A.1-71 name their 1985 authority.
- D-DEBUG-DATA item 5 covers DEBUG-LINE for copied lines.
- The communication devices row names the message delimiter, and a new row covers repeated destinations.
- A new section 3.7.1 records ENTER, group usage DISPLAY, and the external connector's omitted RESERVE.

The doc audits and the Annex A.1 register audit are green.

