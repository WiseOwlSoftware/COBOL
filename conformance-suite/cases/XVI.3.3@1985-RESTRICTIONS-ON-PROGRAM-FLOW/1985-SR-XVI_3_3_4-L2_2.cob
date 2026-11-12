      *>--- conformance-case
      *> id: 1985-SR-XVI_3_3_4-L2_2
      *> rules: [1985-SR-XVI.3.3.4-L2.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XVI.3.3.4 SRL2.2 (RESTRICTIONS ON PROGRAM FLOW). A
      *> refusal: the rule forbids line 15 of the program below, and
      *> WiseOwl refuses it at 85 with COBOL6877 there; a compiler that
      *> accepts it fails. Placed from written and run by the
      *> confirmation pass (the redraft of a WEAK or WRONG record, whose
      *> filed defect was fixed; re-run to MATCH with its expectation
      *> unchanged, witnesses-85g) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SEGSRT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT S ASSIGN TO SORTWK.
       DATA DIVISION.
       FILE SECTION.
       SD  S.
       01  SR.
           02 K PIC X.
       PROCEDURE DIVISION.
       X-SEC SECTION 60.
       X-1.
           SORT S ASCENDING K INPUT PROCEDURE IA THRU IB
               OUTPUT PROCEDURE O.
           STOP RUN.
       O SECTION 60.
       O-1.
           RETURN S AT END GO TO O-9.
           DISPLAY K.
           GO TO O-1.
       O-9.
           EXIT.
       IA SECTION 60.
       IA-1.
           MOVE "B" TO K.
           RELEASE SR.
       IB SECTION 70.
       IB-1.
           MOVE "A" TO K.
           RELEASE SR.
