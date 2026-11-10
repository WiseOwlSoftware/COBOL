      *>--- conformance-case
      *> id: 1985-GR-XI_4_1_4-5
      *> rules: [1985-GR-XI.4.1.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.4.1.4 GR5 (THE MERGE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the witness-drafting pass (a rule the
      *> audit never drafted) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MRGCOL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. X
           PROGRAM COLLATING SEQUENCE IS SEQ-PGM.
       SPECIAL-NAMES.
           ALPHABET SEQ-PGM IS "B" "A" "C"
           ALPHABET SEQ-MRG IS "C" "B" "A".
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "mrgcol1.dat".
           SELECT F2 ASSIGN TO "mrgcol2.dat".
           SELECT SM ASSIGN TO "mrgcols.tmp".
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  F1-REC        PIC X(2).
       FD  F2.
       01  F2-REC        PIC X(2).
       SD  SM.
       01  SM-REC.
           05  S-KEY     PIC X.
           05  S-FILE    PIC X.
       WORKING-STORAGE SECTION.
       01  EOF-SW        PIC X.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT F1.
           MOVE "C1" TO F1-REC WRITE F1-REC.
           MOVE "A1" TO F1-REC WRITE F1-REC.
           CLOSE F1.
           OPEN OUTPUT F2.
           MOVE "B2" TO F2-REC WRITE F2-REC.
           CLOSE F2.
           DISPLAY "COLLATING SEQUENCE PHRASE".
           MERGE SM ON ASCENDING KEY S-KEY
               COLLATING SEQUENCE IS SEQ-MRG
               USING F1 F2
               OUTPUT PROCEDURE IS OP-START THRU OP-END.
           OPEN OUTPUT F1.
           MOVE "B1" TO F1-REC WRITE F1-REC.
           MOVE "C1" TO F1-REC WRITE F1-REC.
           CLOSE F1.
           OPEN OUTPUT F2.
           MOVE "A2" TO F2-REC WRITE F2-REC.
           CLOSE F2.
           DISPLAY "PROGRAM COLLATING SEQUENCE".
           MERGE SM ON ASCENDING KEY S-KEY
               USING F1 F2
               OUTPUT PROCEDURE IS OP-START THRU OP-END.
           STOP RUN.
       OP-START.
           MOVE "N" TO EOF-SW.
       OP-MID.
           PERFORM OP-RET UNTIL EOF-SW = "Y".
       OP-END.
           EXIT.
       OP-RET.
           RETURN SM
               AT END MOVE "Y" TO EOF-SW
               NOT AT END DISPLAY SM-REC
           END-RETURN.
