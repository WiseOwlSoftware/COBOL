- **D-DEBUG-PROGRAM-RULES — the debugging sections' program-level syntax rules** (kb/Work PB5081; the text decides, no latitude). With
  WITH DEBUGGING MODE in force (§1985:XV.2.1.3 GR1) each is a refusal: a debugging section after a declarative section that is not one
  (§1985:XV.3.2.3 SR1, COBOL6948, `ProcedureTableBuilder.DeclCollectSection`); a statement outside the set of debugging sections that
  references a procedure-name of one, PERFORM included (SR3, COBOL6946, `StatementValidation.CheckDeclarativesBoundary`, asking
  `SectionInfo.IsDebugging`); a USE FOR DEBUGGING operand naming a procedure of a debugging section (SR5, COBOL6946); a procedure-name
  named twice (SR6, COBOL6335, the data-, cd- and file-name operands' code); ALL PROCEDURES written twice in the program (SR7) and a
  procedure-name operand in a program with ALL PROCEDURES (SR8), both COBOL6947 (`ProcedureTableBuilder.AddNamedDebugSubject` and
  `FinalizeDebug`). Witnesses: `conformance:cases/XV.3.2@1985-USE-FOR-DEBUGGING-STATEMENT/1985-SR-XV_3_2_3-1`, `-3`, `-5`, `-6`, `-7`, `-8`.
