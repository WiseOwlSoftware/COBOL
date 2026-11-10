      *>--- conformance-case
      *> id: 1985-GR-VII_2_7_4-1
      *> rules: [1985-GR-VII.2.7.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.2.7.4 GR1 (THE PADDING CHARACTER CLAUSE). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the triage of the
      *> drafts that did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PADTST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PFILE ASSIGN TO "PADFILE"
               ORGANIZATION IS SEQUENTIAL
               PADDING CHARACTER IS "*".
       DATA DIVISION.
       FILE SECTION.
       FD  PFILE
           LABEL RECORDS ARE STANDARD
           BLOCK CONTAINS 4 RECORDS.
       01  PREC PIC X(5).
       WORKING-STORAGE SECTION.
       01  EOF-SW PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PFILE.
           MOVE "AAAAA" TO PREC.
           WRITE PREC.
           MOVE "**B**" TO PREC.
           WRITE PREC.
           MOVE "BBBBB" TO PREC.
           WRITE PREC.
           CLOSE PFILE.
           OPEN INPUT PFILE.
           PERFORM READ-PARA UNTIL EOF-SW = "Y".
           CLOSE PFILE.
           DISPLAY "DONE".
           STOP RUN.
       READ-PARA.
           READ PFILE
               AT END MOVE "Y" TO EOF-SW
               NOT AT END DISPLAY PREC
           END-READ.
