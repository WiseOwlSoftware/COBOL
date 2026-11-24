## 2026-11-24 - the sweep's ratchet and the shapes that make the next site automatic (wave 20262165 group A)

**The ratchet this sweep empties** is `tests/WiseOwl.Cobol.Tests.Conformance/edition-citation-sweep.json` (kb/Work PB5922's net at 2002
and 2014): 510 codes at 2002 and 519 at 2014 after this landing (3269 and 3007 rendered lines in the manifest's refusals). A code leaves
when its sites cite their edition; none joins. Measure with `WISEOWL_COBOL_RECORD_CITATION_SWEEP=<dir>` over
`WiseOwlRefusal_RefusesTheSourceWithItsDiagnostic` (each offending line is written to `<dir>/<edition>.txt`).

**The shapes this landing proved**, each one table per clause rather than one argument per site, so the next site is automatic:
`CallSyntaxRules` (the CALL rules by 2023 number, with 2002's renumbering table), `OoConformance.SetRules` (a clause whose rules are
numbered alike), `StorageSectionRule.For` (a rule whose WORDING also differs by edition), `SymbolTable.ClassNames.ParameterizedGround`
(one ground, one citation for every reference site). The sizable families left, by line count at 2002: COBOL1627 (145, intrinsic
argument clauses: a per-edition 15.n paragraph table beside `IntrinsicCatalog.Paragraph1989` would serve every function at once),
COBOL1560 (103, screen-module licence), COBOL1639, COBOL1757, COBOL5506, COBOL2269.

**A tool that finds the counterpart rule** (used here, kept in the wave's scratch): match the 2023 rule's text against the 2002 and 2014
rule books (`cite.rule_book(edition)`) by shared six-word runs; a 1.00 score was the same rule in every case checked. A rule whose text
differs (CALL SR3's section list) still has to be read, and every result goes through `cite.py --check`.
