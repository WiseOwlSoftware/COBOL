      *>--- conformance-case
      *> id: 1985-SR-XVI_3_3_3-1
      *> rules: [1985-SR-XVI.3.3.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XVI.3.3.3 SR1 (RESTRICTIONS ON PROGRAM FLOW). A refusal:
      *> the rule forbids line 31 of the program below, and WiseOwl
      *> refuses it at 85 with COBOL6877 there; a compiler that accepts
      *> it fails. Placed from written and run by the confirmation pass
      *> (the redraft of a WEAK or WRONG record, whose filed defect was
      *> fixed; re-run to MATCH with its expectation unchanged,
      *> witnesses-85g) by scripts/spec/place_witnesses.py (kb/Work
      *> PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SEGMRG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO MRGIN1.
           SELECT F2 ASSIGN TO MRGIN2.
           SELECT M ASSIGN TO MRGWK.
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1 PIC X.
       FD  F2.
       01  R2 PIC X.
       SD  M.
       01  MR.
           02 K PIC X.
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       M-1.
           OPEN OUTPUT F1.
           MOVE "A" TO R1.
           WRITE R1.
           MOVE "C" TO R1.
           WRITE R1.
           CLOSE F1.
           OPEN OUTPUT F2.
           MOVE "B" TO R2.
           WRITE R2.
           CLOSE F2.
           MERGE M ASCENDING K USING F1 F2 OUTPUT PROCEDURE OA THRU OB.
           STOP RUN.
       OA SECTION 10.
       OA-1.
           RETURN M AT END GO TO OB-1.
           DISPLAY K.
           GO TO OA-1.
       OB SECTION 60.
       OB-1.
           DISPLAY "END".
