      *>--- conformance-case
      *> id: g85_f22_03
      *> rules: [1985-SR-VII.4.7.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.7.3 SR3 - ADVANCING mnemonic-name-1 cannot be
      *> specified for a file whose FD has a LINAGE clause. Must be
      *> refused at the WRITE ... ADVANCING TOP-PAGE line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2203.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           C01 IS TOP-PAGE.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT P ASSIGN TO "G85F2203.PRT"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  P
           LABEL RECORDS ARE STANDARD
           LINAGE IS 10 LINES.
       01  PR PIC X(10).
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT P.
           MOVE "LINE" TO PR.
           WRITE PR AFTER ADVANCING TOP-PAGE.
           CLOSE P.
           STOP RUN.
