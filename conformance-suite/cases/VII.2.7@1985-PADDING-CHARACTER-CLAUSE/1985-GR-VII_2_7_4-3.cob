      *>--- conformance-case
      *> id: 1985-GR-VII_2_7_4-3
      *> rules: [1985-GR-VII.2.7.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.2.7.4 GR3 (THE PADDING CHARACTER CLAUSE). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PADTST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PFILE ASSIGN TO PADDISK
               ORGANIZATION IS SEQUENTIAL
               PADDING CHARACTER IS PAD-CHAR.
       DATA DIVISION.
       FILE SECTION.
       FD  PFILE
           BLOCK CONTAINS 3 RECORDS.
       01  PREC PIC X(5).
       WORKING-STORAGE SECTION.
       01  PAD-CHAR PIC X VALUE "*".
       01  EOF-FLAG PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "#" TO PAD-CHAR.
           OPEN OUTPUT PFILE.
           MOVE "*" TO PAD-CHAR.
           MOVE "AAAAA" TO PREC.
           WRITE PREC.
           MOVE "BBBBB" TO PREC.
           WRITE PREC.
           CLOSE PFILE.
           MOVE "#" TO PAD-CHAR.
           OPEN INPUT PFILE.
           PERFORM UNTIL EOF-FLAG = "Y"
               READ PFILE
                   AT END MOVE "Y" TO EOF-FLAG
                   NOT AT END DISPLAY PREC
               END-READ
           END-PERFORM.
           CLOSE PFILE.
           DISPLAY "DONE".
           STOP RUN.
