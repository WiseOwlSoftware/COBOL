## Entry 2785 — 2026-10-30 10:53 PDT — PB596 discharged: the shared level-3 diagnostic already names its own statement's clause, now pinned end to end

Wave 20261770, group D (the EcNameResolution root). PB596 said `EcNameResolution` hard-codes RAISE's §14.9.29.3 SR1 in the
COBOL0710 message for every raising context, so `EXIT PROGRAM RAISING EC-BOUND` was pointed at the wrong statement.

**Re-probe (Debug build of this tree, COBOL-2023).** It does not reproduce. `EcRaiseSite` (PB388) carries the verb, the clause
and each rule ordinal as one value per statement, `TryResolve` prints `site.Cite(site.Level3Rule)`, and `EcBindRaising` cites the
EC-USER leg from the same site. Measured: EXIT PROGRAM -> `ISO §14.9.14.3 SR3`, GOBACK -> `§14.9.18.3 SR2`, RAISE ->
`§14.9.29.3 SR1`; COBOL0717 (EC-USER absent from the PD header) -> `§14.9.14.3 SR3` for EXIT. The two EXIT diagnostics.json
`expect` strings already carry the corrected clause, and `EcRaiseSiteDriftTests` re-derives every ordinal from the rule catalog
(each `cite.py --check` on 14.9.14.3 SR3, 14.9.18.3 SR2, 14.9.29.3 SR1 returned OK). The other `TryResolve` callers (USE, the
PERFORM WHEN list, the declined-facility binder) pass no level-3 site, so no fourth literal exists.

**What was missing and is added.** The end-to-end pin the note asked for: `ExceptionConditionConformanceTests.
RaisingContext_DiagnosticCitesItsOwnClause` asserts, for RAISE, GOBACK and EXIT PROGRAM (COBOL0710) and for GOBACK and EXIT
PROGRAM (COBOL0717), that the diagnostic line carries its own statement's clause. A future fourth raising context whose message
reverts to a literal fails there.

**Register.** PB596 flipped to `landed`; `SR-14.9.14.3-3` (already CONFORMS) moved from `inventory_rows` to `closes_rows` and
gained the two new witnesses and the real implementing sites through a `record_verdicts` batch (GAP 20 -> 20).

**Self-review.** Architecture: no new mechanism, the existing `EcRaiseSite` is the one carrier. Full code: the test cites the
clauses the code cites. Performance: test-only. Duplication: none added.

