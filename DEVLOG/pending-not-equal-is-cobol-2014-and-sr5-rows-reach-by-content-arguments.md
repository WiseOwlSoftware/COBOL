## The `<>` operator is a COBOL-2014 introduction, and §14.9.25.3 SR5's edition rows reach a figurative argument BY CONTENT (PB3604, PB3755)

Two under-rejects in the edition gates, found by the 2026-10-23 edge sweep (PB3602) and by PB3605's review, both confirmed on
this build before any change: `IF A <> 3` compiled at `--std 2002` (PB3604), and HIGH-VALUE, LOW-VALUE, QUOTE and ALL "X" passed
BY CONTENT into a numeric-edited formal compiled clean at `--std 2023`, where the equivalent MOVE is COBOLNET0902 (PB3755).

**PB3604, the `<>` edge.** The row was a derivation from silence ("Annex E lists no `<>`, so it predates 2023; 2002 is the lowest
certain edition"). The archived texts overturn it (`research/pb296-dl`): the post-ballot 2002 text's clause 8.7.5.2 ends its
abbreviation rules at SR8 (`<=`) and its special character words read `>= <= *> >> ::` with no `<>`, and Annex D.3 (2002->2014
changes) item 16 of the 2009 committee draft reads "Relational operator enhancement. The symbol <> has been added as an extended
relational operator." The row is now `not-equal-operator-2014` (renamed in every caller: the generated registry, the parse arm, VCR
row 7.26 and its generated index, the goldens, the inventory). `cite.py --check` reports SR8 for the SR11 words (PB3606), so SR11 is
confirmed from `--find`. A sibling arm surfaced on the way: the symbol is also written in a `>>IF` / `>>EVALUATE`
constant-conditional-expression (§7.3.8.2 SR1 a)), a fragment the conditional-compilation stage parses apart and the
compilation-unit walk never reaches, and that lane had no gate (`>>IF 5 <> 3` compiled at 2002). The gate is now one body,
`RelationalOperatorGate` (the `LogicalOperatorGate` precedent), asked per `comparisonOperator` node by the parse arm and per
evaluated fragment by `DirectiveDiag.GateRelationalOperators`; a compiled branch only, like the XOR and boolean-operator gates.
`RelationalOperatorGateDriftTests` pins that the row is read only by the body, both lanes call it, and the NOTEQUAL token is
spelled by one grammar rule. Five positives moved to `accept: [2014]` (`l1c26_relational_operator_not_equal_symbol`,
`pb1034_is_not_equal_symbol`, `pb1034_cce_printed_relational_operators`, `pb1021_address_identifier_operands`,
`pb1391_boolean_relations_stated_in_full`); the negative was renamed `pb1459-not-equal-operator-below-2014` (85 and 2002) and
`pb3604-cce-not-equal-operator-below-2014` (2002) covers the directive lane. The word forms `NOT EQUAL TO` and `NOT =` stay legal
at 85 (probed).

**PB3755, SR5's rows and the activation arm.** §14.8.2.3.3 rule 2 d): for a formal that is neither numeric, index nor ANY LENGTH
"the conformance rules are the same as for a MOVE statement". The rows of §14.9.25.3 SR5 had two askers (the bound MOVE and the
bound INITIALIZE) in a private method of `VersionConformancePass`, so a CALL, an INVOKE or a user-defined function with a
2023-removed figurative into a numeric-edited formal compiled clean, and so did ALL "57" (8.3.3.6.3 SR3, removed 2002). A numeric
formal is rule 2 a)'s COMPUTE and was already refused. The note's direction ("make the activation arm a third asker") was taken as
the shape rather than the diff: the three rows left the pass for ONE public body, `FigurativeAssociation.GateSr5` (beside
`GateSr3`, which it asks first), with the receiver exemptions in `Sr5ReceiverOf`, and `GateMove`, `GateInitialize` and
`ParameterConformance.GateFigurativeEdition` call it. The activation arm asks after the edition-blind relation passed, from
`CheckArgument` (CALL and function, the entry both share) and the INVOKE figurative arm; all three lanes measured: valid at 2002 and
2014, COBOLNET0902 at 2023 (QUOTE the 0903 warning at 2014), ALL "57" refused from 2002. A call located by name at run time has no
compile-time formal, so it has nothing to gate. The three `EditionGateArmDriftTests` bound-arm entries left with the rows;
`Sr5EditionRowAskerDriftTests` pins that the rows are read only by the body and that every file asking
`ContentConformanceReason` also asks `GateFigurativeEdition` (mutation-checked: renaming either call fails the test).
Goldens: `pb3755_content_figurative_numeric_edited_formal` (2002, 2014; all three lanes), a 2023 negative per lane
(`pb3755-call-...`, `pb3755-invoke-...`, `pb3755-function-...`) and `pb3755-content-all-multichar-numeric-edited-2002`.

**The carried predecessor.** This branch carries `worktree-wf_1341dc87-1c4-14` (PB1601, PB1615), merged by the merge driver. Its
literal-text-name gate refuses `COPY "dbg.cpy"` at 85, and `DiagnosticPositionTests`' 85 debugging-line copybook case wrote exactly
that; its own gate had not run the case (not in leg 1), mine did. The case names its copybook as a word now.

**Self-review** (pr-test-analyzer, silent-failure-hunter, comment-analyzer lenses over the diff): every early return of
`GateFigurativeEdition` is a documented "SR5 does not reach it" (not a figurative, not numeric-edited); the directive gate is silent in
an omitted branch by the same rule as its XOR and boolean twins; the stale prose naming `VersionConformancePass.GateSr5` in
`MoveTable16`, `MoveValidity`, `FigurativeAssociation`, `COBOLNET_FILES_DESIGN` and VCR row 7.13 was rewritten; both new drift tests
were made to fail once for the right reason.

**Inventory:** rows SR-8.3.2.4.2-2, SR-8.7.5.2-11 (editions now 2014,2023), SR-14.9.25.3-5, SR-8.3.3.6.3-3 and GR-14.8.2.3.3-L2.2
re-verdicted (witnesses moved and added; 3 retirements); GAP 34 -> 34, no row changed state. PB3604 and PB3755 landed with
`closes_rows: []` and the reason.

**Gate:** `build-local` run `000003-20261025T231434Z` at head 3e897dc63: `LEG 1 ONLY (batched gating, PB2515): GREEN`, Linux half (WSL clone of HEAD) GREEN, Conformance 8,289/18,656, Unit 36,729/38,311, Characterization 8/36 in leg 1. The two earlier gates (`000001`, `000002`) were RED on the one carried case above (first a literal text-name at 85, then the host folding a word text-name's file name to upper case); both are fixed, and the whole local Conformance (18,656) and Unit (38,346) assemblies ran green before the third gate. Every golden added or moved ran in leg 1 (e.g. `SuiteTest_IsNoWorseThanItsVerdictAllows(edition 2014, id pb3755_content_figurative_numeric_edited_formal)` Passed, `WiseOwlRefusal_RefusesTheSourceWithItsDiagnostic(id pb3604-cce-not-equal-operator-below-2014, edition 2002)` Passed).
