      *>--- conformance-case
      *> id: 1985-SR-XVI_3_3_3-p1
      *> rules: [1985-SR-XVI.3.3.3-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XVI.3.3.3 p1 (RESTRICTIONS ON PROGRAM FLOW). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-19.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * s-19 p13a: MERGE in non-independent SECTION 10 with its
      * OUTPUT PROCEDURE wholly in a single independent segment, 70
      * (1985 XVI.3.3.3 p1, SR2). Expect: O70 AAAA, BBBB, CCCC, DONE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S19P13A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IN1 ASSIGN TO "S19MIN1".
           SELECT IN2 ASSIGN TO "S19MIN2".
           SELECT SD-FILE ASSIGN TO "S19MWORK".
       DATA DIVISION.
       FILE SECTION.
       FD  IN1.
       01  IN1-REC PIC X(4).
       FD  IN2.
       01  IN2-REC PIC X(4).
       SD  SD-FILE.
       01  SD-REC.
           05 SD-KEY PIC X(4).
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       M-1.
           OPEN OUTPUT IN1 IN2.
           MOVE "AAAA" TO IN1-REC. WRITE IN1-REC.
           MOVE "CCCC" TO IN1-REC. WRITE IN1-REC.
           MOVE "BBBB" TO IN2-REC. WRITE IN2-REC.
           CLOSE IN1 IN2.
           GO TO MRG-1.
       MRG-SEC SECTION 10.
       MRG-1.
           MERGE SD-FILE ON ASCENDING KEY SD-KEY
               USING IN1 IN2
               OUTPUT PROCEDURE IS O70.
           DISPLAY "DONE".
           STOP RUN.
       FIX-SEC SECTION 20.
       FIX-1.
           DISPLAY "NOT RUN".
       O70 SECTION 70.
       OUT-1.
           RETURN SD-FILE AT END GO TO OUT-X.
           DISPLAY "O70 " SD-KEY.
           GO TO OUT-1.
       OUT-X.
           EXIT.
