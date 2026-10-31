*>--- conformance-case
*> id: pb4162_table_all_dynamic_capacity_held_below_2014
*> rules: [GR-8.4.3.2.4-2, AR-15.3-14]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4162 - the negative of pb4162_table_all_dynamic_capacity_held_for_order. A dynamic-capacity table (the OCCURS
      *> DYNAMIC clause, ISO §13.18.38 Format 4) is a COBOL-2014 introduction, so the program is refused by name at 2002 and
      *> 85 - COBOL0900, the introduction gate (at 85 the user-defined function reference is the first refusal).
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB4162M.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(8).
       01 L-R PIC 9(8).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       P.
           MOVE L-X TO L-R.
           GOBACK.
       END FUNCTION PB4162M.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4162NEGDYN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PB4162M
           FUNCTION MAX INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-T.
          05 WS-D OCCURS DYNAMIC PIC 9(3).
       01 WS-LO PIC 9(8) VALUE 0.
       01 WS-R  PIC 9(8).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE WS-R = FUNCTION MAX (WS-D (ALL), FUNCTION PB4162M (WS-LO))
           STOP RUN.
       END PROGRAM PB4162NEGDYN.
