- **D-DEBUG-USE — how USE FOR DEBUGGING spells its data-item operand** (kb/Work PB4237). ISO/IEC 1989:2023 prints no
  debug format (the facility left in 2002, VCR Table 7 row 7.17), and the 1985 text is not in the repository, so the
  spelling follows rule 1's precedence to GnuCOBOL. **Determination:** in `[ALL REFERENCES OF] identifier-1` only ALL is
  a keyword; REFERENCES and OF are optional words, so `ALL W-A`, `ALL OF W-A`, `ALL REFERENCES W-A` and `ALL REFERENCES
  OF W-A` are one operand (GnuCOBOL 3.2 `cobc -std=cobol85 -fsyntax-only` accepts all four; CCVS DB201A writes `ALL
  ID-2` and `ALL AB2 OF A2`). The grammar is `useDebugTarget`'s `ALL REFERENCES? OF? dataReference` arm, and the binder
  tells the arm by ALL, never by REFERENCES, so `ALL P1` is never a procedure-name subject. Without WITH DEBUGGING MODE
  the section is inert; with it every operand is RESOLVED before it is named (kb/Work PB4363): a data item is a trigger target
  (D-DEBUG-DATA; it is judged by its DECLARATION, so an unsubscripted table element, the form DB201A writes, is one and never
  draws COBOL2270); in the ALL arm a word that names a paragraph or section is
  COBOL5848 (a procedure-name identifies no data item, ISO 14.4.1 with 8.4.2.1); a word nothing declares is the one
  undefined-reference COBOL1639 in either arm (8.4.2.1), the program being wrong and not the compiler incomplete (a paragraph under a qualifier that is not its own section
  is COBOL1639 in the procedure-name text, `names no procedure`). A cd-name the program declares (X3.23-1985's debug module
  triggers on the ENABLE, DISABLE, SEND, RECEIVE and ACCEPT MESSAGE COUNT of one; CCVS DB205A) is a bare operand like a file-name,
  a cd-name trigger target (D-DEBUG-DATA) and never "not defined" (kb/Work PB4713); a file-name is a file-name trigger target
  (D-DEBUG-FILE, kb/Work PB4811); after ALL it is no identifier-1, and the resolver says it is
  "declared as a cd-name, not as a data item" (COBOL1639), as it does for `MOVE CM-IN TO T`. The
  bare operand reads `word` and `word OF section` as a procedure-name first (8.4.2.2.2), so `ON PARA-W OF SEC-A`
  debugs the qualified paragraph. Witnesses: `conformance:cases/_unassigned/pb4237_debug_all_identifier` (85),
  `conformance:cases/_unassigned/pb4237-debug-all-identifier-removed-2002` (COBOL0902 from 2002),
  `conformance:cases/_unassigned/pb4363_debug_qualified_procedure` (85),
  `conformance:cases/_unassigned/pb4363-debug-all-procedure-name` (COBOL5848 at 85),
  `conformance:cases/_unassigned/pb4363-debug-all-undeclared` (COBOL1639 at 85),
  `conformance:cases/_unassigned/pb4713-debug-cd-name` (accepted at 85), `conformance:cases/_unassigned/pb4713-debug-all-cd-name`
  (COBOL1639 at 85), `conformance:cases/_unassigned/pb4713-move-cd-name` (COBOL1639 at 85 and 2002),
  `conformance-test:DebugFacilityTests.BareOperandNamingACdName_At85_IsTheCdNameSubject`,
  `conformance-test:DebugFacilityTests.AllOperandNamingACdName_At85_IsDeclaredAsACdName`,
  `conformance-test:DebugFacilityTests.AllIdentifier_EverySpelling_At85_IsTheDataItemOperand`,
  `conformance-test:DebugFacilityTests.OperandNamingAnUnsubscriptedTableElement_At85_IsTheElementSubject`,
  `conformance-test:DebugFacilityTests.ProcedureNameUnderTheWrongQualifier_At85_NamesNoProcedure`.
