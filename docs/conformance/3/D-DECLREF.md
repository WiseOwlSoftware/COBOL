- **D-DECLREF — the declaratives reference boundary: an SR4 violation is an error, an SR3 violation is a warning
  (kb/Work PB362).** §2023:14.9.49.3 SR3 ("Within a declarative procedure, there shall be no reference to any
  nondeclarative procedures except in a RESUME statement") and SR4 ("Procedure-names within a declarative section
  may be referenced in a different declarative section or in a nondeclarative procedure only with a PERFORM
  statement") are syntax rules. For a syntax rule the standard requires only §2023:4.2.2's "warning mechanism", so the
  standard settles WHETHER a reference violates the boundary, but not the severity. The severity is therefore
  implementor latitude, and the owner's precedence (ISO, then GnuCOBOL, then IBM or Micro Focus) decides it. An
  **SR4** violation is an **ERROR, COBOL2376**. A GO TO, an ALTER, or a SORT/MERGE INPUT or OUTPUT PROCEDURE
  phrase that enters a USE procedure from outside its section has no activating USE for §2023:14.9.49.4 GR7's return,
  and GnuCOBOL refuses a reference into DECLARATIVES from outside them. An **SR3** violation is a **WARNING,
  COBOL2375**, as it is in GnuCOBOL. A PERFORM or GO TO of a nondeclarative procedure from a USE procedure runs
  as written, is common in production source, and IBM Enterprise COBOL and Micro Focus accept it without comment.
  ⚠ The GnuCOBOL postures are recalled, not measured: the implementer host has no GnuCOBOL install, and the
  differential run is where they are confirmed. Both rules are asked of SECTION identity in the one
  procedure-name funnel (`ProcedureTableBuilder.ResolveProcedureOperand` →
  `StatementValidation.CheckDeclarativesBoundary`), so every statement that takes a procedure-name inherits them.
  Only PERFORM and RESUME claim an exemption. Pinned by `unit:DeclarativesStructuralRulesTests`,
  `conformance:cases/14.9.49-USE/pb362_declaratives_reference_boundary` and `conformance:cases/14.9.49-USE/pb362-goto-into-declarative`.
  **At 85 (kb/Work PB4995).** SR3's ancestors are five 1985 rules: the USE statement's GR2 in four modules,
  §1985:VII.4.6.4 GR2, §1985:VIII.4.8.4 GR2, §1985:IX.4.8.4 GR2 and §1985:XIII.4.8.4 GR2 ("Within a declarative
  procedure, there must be no reference to any nondeclarative procedures"), and the Debug module's §1985:XV.3.2.3 SR2
  ("Except in the USE FOR DEBUGGING statement itself, there must be no reference to any non-declarative procedure
  within the debugging section"). WiseOwl reports each violation at `--std 85` as the same **WARNING, COBOL2375**, and
  compiles the program. The basis is §1985:I.1.6: "The results of violating the formats or rules of Standard COBOL are
  undefined unless otherwise specified in this standard", and none of the five specifies otherwise, so a 1985 compiler
  need not refuse the program. Witnessed by the accepted 85 tests `conformance:cases/VII.4.6@1985-USE-STATEMENT/1985-GR-VII_4_6_4-2`,
  `…/VIII.4.8@1985-USE-STATEMENT/1985-GR-VIII_4_8_4-2`, `…/IX.4.8@1985-USE-STATEMENT/1985-GR-IX_4_8_4-2`,
  `…/XIII.4.8@1985-USE-AFTER-STANDARD-EXCEPTION-PROCEDURE-STATEMENT/1985-GR-XIII_4_8_4-2` and
  `…/XV.3.2@1985-USE-FOR-DEBUGGING-STATEMENT/1985-GR-XV_3_2_4-1`. The warning's own pins wait on kb/Work PB5461:
  the suite cannot yet pin a warning on a program it accepts.
