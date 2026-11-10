      *>--- conformance-case
      *> id: 1985-GR-IV_6_4_2_1_1_1-1
      *> rules: [1985-GR-IV.6.4.2.1.1.1-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.4.2.1.1.1 GR1 (Definition of Conditional Statement).
      *> An output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RDCOND.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT INFILE ASSIGN TO "RDCONDF"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  INFILE
           LABEL RECORDS ARE STANDARD.
       01  IN-REC PIC X(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT INFILE.
           MOVE "HELLO" TO IN-REC.
           WRITE IN-REC.
           CLOSE INFILE.
           OPEN INPUT INFILE.
           READ INFILE AT END DISPLAY "EOF 1"
               NOT AT END DISPLAY IN-REC
           END-READ.
           READ INFILE AT END DISPLAY "EOF 2"
               NOT AT END DISPLAY "UNEXPECTED"
           END-READ.
           CLOSE INFILE.
           STOP RUN.
