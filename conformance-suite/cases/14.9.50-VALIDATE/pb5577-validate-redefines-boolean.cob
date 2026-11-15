      *>--- conformance-case
      *> id: pb5577-validate-redefines-boolean
      *> rules: [GR-14.9.50.4-2, GR-14.9.50.4-6]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO/IEC 1989:2002 §13.16.42.3 GR3 (REDEFINES clause): "If a REDEFINES clause is specified in the data description entry for a
      *> data item subordinate to the operand of a VALIDATE statement, each definition is used by the VALIDATE statement independently
      *> of the others, subjecting the same data locations to more than one set of checks." Here F is two characters and FN redefines
      *> them as PIC 99, so "AB" passes F (any content) and fails FN's PICTURE; BT is PIC 1, a boolean item whose content shall be
      *> 0 or 1 (§2023:8.8.4.4.4 GR3 e, the class BOOLEAN; format validation GR6 a checks "compatibility with the PICTURE").
      *>   cite.py: OK  §2002:13.16.42.3 3)  (General rules)
      *> WHY EACH LEG CAN FAIL: a walk that skipped the REDEFINES entry would print no line for "AB1"; one that skipped the boolean
      *> item would print none for "122"; one that stopped at the first invalid element would print two lines for "AB2".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5577B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  R.
           05  F     PIC X(2).
           05  FN    REDEFINES F PIC 99.
           05  BT    PIC 1.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-SEC SECTION.
           USE AFTER EXCEPTION CONDITION EC-VALIDATE-FORMAT.
       D-PARA.
           DISPLAY "  EC-VALIDATE-FORMAT".
       END DECLARATIVES.
       MAIN SECTION.
       M-PARA.
           >>TURN EC-VALIDATE-FORMAT CHECKING ON
           DISPLAY "121: valid"
           MOVE "121" TO R
           VALIDATE R
           DISPLAY "AB1: FN invalid"
           MOVE "AB1" TO R
           VALIDATE R
           DISPLAY "122: BT invalid"
           MOVE "122" TO R
           VALIDATE R
           DISPLAY "AB2: both invalid, one condition per statement"
           MOVE "AB2" TO R
           VALIDATE R
           DISPLAY "done"
           STOP RUN.
