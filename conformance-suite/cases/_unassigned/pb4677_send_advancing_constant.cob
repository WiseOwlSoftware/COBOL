      *>--- conformance-case
      *> id: pb4677_send_advancing_constant
      *> rules: []
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4677 - ISO 13.10.3 SR2 at COBOL-2002: a constant-name stands where a format
      *> specifies a literal, here integer-1 of SEND's ADVANCING phrase (ISO/IEC 1989:2002 A.2.6.1).
      *> An unknown destination answers status 20 at any MCS, and a phrase the binder refuses
      *> prints nothing.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4677AC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TWO-LINES CONSTANT AS 2.
       01 W-TEXT PIC X(5) VALUE "HELLO".
       COMMUNICATION SECTION.
       CD COUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS OL
           STATUS KEY IS OK
           SYMBOLIC DESTINATION IS OD.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 1 TO DC.
           MOVE 5 TO OL.
           MOVE "NOSUCHDEST" TO OD.
           SEND COUT FROM W-TEXT WITH EMI AFTER ADVANCING TWO-LINES LINES.
           DISPLAY "SEND " OK.
           STOP RUN.
