      *>--- conformance-case
      *> id: g85_f22_04
      *> rules: [1985-SR-VII.4.7.3-8]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.7.3 SR8 - END-OF-PAGE requires a LINAGE clause
      *> in the FD. This FD has none: must be refused at the WRITE
      *> ... AT END-OF-PAGE line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2204.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT P ASSIGN TO "G85F2204.PRT"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  P
           LABEL RECORDS ARE STANDARD.
       01  PR PIC X(10).
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT P.
           MOVE "LINE" TO PR.
           WRITE PR AT END-OF-PAGE DISPLAY "EOP".
           CLOSE P.
           STOP RUN.
