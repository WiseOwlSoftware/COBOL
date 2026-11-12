      *>--- conformance-case
      *> id: 1985-SR-XVI_3_3_3-L2_2
      *> rules: [1985-SR-XVI.3.3.3-L2.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XVI.3.3.3 SRL2.2 (RESTRICTIONS ON PROGRAM FLOW). A
      *> refusal: the rule forbids line 23 of the program below, and
      *> WiseOwl refuses it at 85 with COBOL6877 there; a compiler that
      *> accepts it fails. Placed from written and run by the 1985 draft
      *> triage (witnesses-85k): a draft that still mismatched in
      *> rerun-85g, corrected from the rule's text and run to MATCH by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SEGMRG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IN1 ASSIGN TO "IN1".
           SELECT IN2 ASSIGN TO "IN2".
           SELECT SW ASSIGN TO "SW".
       DATA DIVISION.
       FILE SECTION.
       FD  IN1.
       01  IN1-REC PIC X(10).
       FD  IN2.
       01  IN2-REC PIC X(10).
       SD  SW.
       01  SW-REC.
           05  SW-KEY PIC X(10).
       PROCEDURE DIVISION.
       MAIN-SEC SECTION 50.
       M-1.
           MERGE SW ON ASCENDING KEY SW-KEY
               USING IN1 IN2
               OUTPUT PROCEDURE IS OUT-SEC.
           STOP RUN.
       OUT-SEC SECTION 60.
       O-1.
           RETURN SW AT END GO TO O-9.
           GO TO O-1.
       O-9.
           EXIT.
