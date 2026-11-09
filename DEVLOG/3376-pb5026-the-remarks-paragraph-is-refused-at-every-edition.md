## Entry 3376 — 2026-11-09 04:35 PST — PB5026: the REMARKS paragraph is refused at every edition, because no held edition has it

**The defect.** `constructs.json` row `remarks-removed-2002` opened the REMARKS paragraph's window at 85 as "a '74
carryover accepted at 85 for CCVS", so `--std 85` compiled and ran `REMARKS. ...` after PROGRAM-ID. Re-probed on this
branch's base (c865ee6dc3): the PB4996 repro printed `RAN` at `--std 85` and drew COBOL0902 "removed in COBOL-2002" at
`--std 2002`.

**What the standard says.** The X3.23-1985 Identification Division general format (§1985:VI.3.2.1.1) prints
PROGRAM-ID, AUTHOR, INSTALLATION, DATE-WRITTEN, DATE-COMPILED and SECURITY, and §1985:VI.3.2.1 says that format
"defines the order of presentation in the source program". REMARKS appears in the 1985 text only in the history of
COBOL, §1985:A.2.5 item 8: the CODASYL Journal of Development 1968 deleted "NOTE and REMARKS in favor of a general
comment capability for all divisions". The ISO 2002 and 2023 texts carry no REMARKS. The CCVS carve-out protected
nothing: no NIST program writes the paragraph (the matches are a procedure paragraph `REMARKS-TEST` and a literal).
The five sibling paragraphs are in the 1985 figure, so `identification-comments-removed-2002` stays as it is.

**The fix.** The row becomes `remarks-removed-85`, `introducedIn` 85 and `removedIn` 85: an empty window, so the one
gate (`VersionConformancePass.ParseArm.VisitRemarksParagraph`, through `ConstructRegistry.Check`) refuses the paragraph
at every edition with COBOL0902 "removed in COBOL-85", and `--permissive` warns and compiles, as for every removed
construct. The grammar keeps the `remarksParagraph` rule so the refusal names the paragraph rather than a parse error.
`edition_gates.HELD_OPEN` loses its REMARKS entry: a window that starts and ends at the oldest held edition has no edge
inside any pair, and the audit treats a held entry that finds nothing as a finding. The comments and documents that
called REMARKS an 85 carryover (the grammar, `ClauseCardinality`, `DivisionCursor`, VCR row 7.10,
VERSION_TEST_MATRIX_DESIGN, CONFORMANCE D-CE1, DESIGN-frontend-grammar, the construct catalogue) now say what the text
says. VCR row 7.10 and `ClauseCardinality` also drop their "derived, not quoted" warning: the 1985 figure is in the
repository now, and it matches the derivation.

**Goldens.** `remarks-paragraph` moves from `_unassigned` to `cases/11.2-Identification-division-structure/` with its
citations, refused at 85, 2002, 2014 and 2023 with "COBOL0902: the REMARKS paragraph was removed in COBOL-85";
`pb1758-fixed-remarks-at-2023` (fixed form) is refused at 85 too. The version matrix row runs the refusal and the
permissive warning at each edition. The first gate was RED on two goldens that wrote an IDENTIFICATION DIVISION REMARKS
at 85 and that the first sweep (a grep for `REMARKS.`) missed: `wiseowl/.../pb3245_free_comment_entry_is_commentary`
wrote `REMARKS no period ...`, so its no-period leg moves to SECURITY; and
`pb4487_library_text_copied_twice_is_decided_per_inclusion` copies a REMARKS library text into both divisions. That
program now has no conforming edition, so it becomes a refusal at all four (one COBOL0902, at the first inclusion only),
and its per-inclusion decision stays held by `CommentEntryResolverTests`' PB4487 facts.

**Rows.** None re-verdicted: the 1985 inventory carries lineage only (1985-FMT-VI.3.2.1.1 CHANGED to FMT-11.2.1,
unchanged), and no 2023 row covers a paragraph no edition has.

