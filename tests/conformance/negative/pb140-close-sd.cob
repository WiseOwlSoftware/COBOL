      *> reject-at: 2023
      *> ISO 13.4.6.3 SR3: a sort-merge file-name shall not be specified in
      *> an input-output statement - CLOSE included (kb/Work PB140: it
      *> previously ran against an unregistered connector, fail-open '00').
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB140N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT S ASSIGN TO "s.dat".
       DATA DIVISION.
       FILE SECTION.
       SD S.
       01 SREC PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           CLOSE S
           STOP RUN.
