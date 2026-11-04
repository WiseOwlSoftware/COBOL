*>--- conformance-case
*> id: pb4638-typedef-same-as-group-one-refusal
*> rules: [SR-13.16.3-12]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 13.16.3 SR12: "The SAME AS clause shall not be specified in the same data description
      *> entry with any clauses except CONSTANT RECORD, entry-name, EXTERNAL, GLOBAL, level-number, and OCCURS."
      *> TYPEDEF is not among them, so entry T has exactly ONE defect: COBOL1555.
      *> The recovery keeps the SAME AS reference and the TYPEDEF role (kb/Work PB4602), and the type declaration
      *> is then described by Z as a legal entry would be (13.18.49.4 GR1: "as though the data description
      *> identified by data-name-1 had been coded in place of the SAME AS clause").  Y TYPE T therefore has the
      *> subordinate ZA (13.18.57.4 GR2) and the qualified references to it draw nothing (kb/Work PB4638): the former
      *> recovery expanded the declaration's members but never its own SAME AS, so each ZA OF Y drew a false COBOL1639
      *> ("does not uniquely identify a data item").  The one-diagnostic count is pinned by
      *> ReferenceCompositionRecoveryTests.SameAsGroupWithTypedef_SubordinatesOfTheTypeResolve.
      *> reject-at omits 85 because SAME AS and TYPE are COBOL-2002 introductions there (COBOL0900).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4638TS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Z.
          05 ZA PIC X.
          05 ZB PIC 9.
       01 T TYPEDEF SAME AS Z.
       01 Y TYPE T.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "A" TO ZA OF Y
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
