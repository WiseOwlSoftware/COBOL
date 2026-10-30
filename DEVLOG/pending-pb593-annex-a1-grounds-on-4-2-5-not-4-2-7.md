## An Annex A.1 item is grounded on §4.2.5, not §4.2.7, and the audit now refuses the other

kb/Work PB593 was a citation that passed `cite.py --check` and was still wrong. The inventory schema grounded the
"optional element not provided" verdict (`DOCUMENTED-NON-SUPPORT`, owner decision PB280 Q1) partly on §4.2.7: "An
implementor shall identify in user documentation the optional language elements for which that implementor claims
support." The sentence is in §4.2.7, so the check says OK. But §4.2.7's first sentence scopes it to Annex A.4, the
optional language element list. The clause that governs Annex A.1 is §4.2.5, whose first paragraph applies it "to
optional language elements for which an implementor claims support". That is CLAUDE.md rule 1's inheritance failure
mode again: the number was copied from the schema into row notes and the register, never re-derived.

**Re-probe.** Two of the note's three sites had already moved. PB592 rewrote `DOC-A.1-206` and says so in its notes
("the ground named here is 4.2.5, NOT 4.2.7"); `DOC-A.1-127` carries no 4.2.7 text; PB373's spec_refs already lists
4.2.5. The schema still named 4.2.7 twice, and the sweep found copies the note did not list: `DOC-A.1-150`'s notes,
`GR-12.4.5.11.4-4` and `-6`, `docs/CONFORMANCE.md` §7's preamble paragraph and item 150's row, PB292's spec_refs and
PB538's "Expected" line.

**Fix.** The ground is now §4.2.5 ¶1 read with A.1's own preamble, in the schema's `meaning` and in the
`a1-optional-not-provided` decision, in the §7 preamble and item 150's row, and in the three inventory notes (one
`record_verdicts` batch, three rows, GAP 20 to 20). PB280 carries a dated correction. Every Annex A.4 use of §4.2.7
(screen handling, FORMAT / SELECT WHEN, VALIDATE, the OO items, the diagnostics' PostureClause) is right and untouched. No
row was re-verdicted, so `closes_rows` is empty with its reason.

**Guard.** `audit_annex_a1.py` gained `inherited_ground_findings` / `sweep_inherited_grounds`: a §4.2.7 mention in a DOC
row's notes, or beside an A.1 naming anywhere in CONFORMANCE.md, the inventory notes or the schema prose, must have
Annex A.4 named within 200 characters or at the head of its row. Run against the pre-fix tree it reports 8 findings; on the
fixed tree, none. `--self-test` drives it through ten cases that each fail for their own reason (an A.4 row passes, an A.1
ground fails, a far-away A.4 does not rescue it, a wrapped blockquote is read whole, `8.8.4.2.7` is not a mention).
`AnnexA1RegisterDriftTests` already runs the audit every build, so no C# changed.

Self-review (architecture, full code, performance, duplication): one rule in one place (the audit script, beside the other
register checks); the check reads the schema through `SCHEMA_PATH` rather than a second copy; no hot path. The guard is a
proximity heuristic by design, so a future prose that cites 4.2.7 for an A.1 item with an unrelated "A.4" nearby would pass;
that trade is named in the function's docstring.
