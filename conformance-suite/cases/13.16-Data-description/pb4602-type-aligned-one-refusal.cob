*>--- conformance-case
*> id: pb4602-type-aligned-one-refusal
*> rules: [SR-13.16.3-14]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 13.16.3 SR14: "The TYPE clause shall not be specified in the same data description
      *> entry with any clauses except BASED, CLASS, CONSTANT RECORD, DEFAULT, DESTINATION, entry-name,
      *> EXTERNAL, GLOBAL, INVALID, level-number, OCCURS, PRESENT WHEN, PROPERTY, TYPEDEF, VALIDATE-STATUS,
      *> VALUE, and VARYING."  ALIGNED is not among them, so entry X has exactly ONE defect: COBOL2150.
      *> The recovery drops the refused co-clause and keeps the TYPE reference, so X is described by T
      *> (13.18.57.4 GR1) like a legal entry and draws no second diagnostic (kb/Work PB4602): the former recovery
      *> cleared the TYPE clause, and X, a PICTURE-less leaf, drew a false COBOL1942 ("not an elementary or group
      *> data item") and a COBOL0881 as well. The one-diagnostic count is pinned by ReferenceCompositionRecoveryTests.
      *> reject-at omits 85 because the TYPE clause is itself a COBOL-2002 introduction there (COBOL0900).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4602TA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T TYPEDEF STRONG.
          05 A PIC X.
       01 X TYPE T ALIGNED.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
