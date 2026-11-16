      *>--- conformance-case
      *> id: g2-qual-linage-counter-two-fds-unqualified
      *> rules: [2002-SR-8.4.1.1.2-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.4.1.1.2 SR6 - "LINAGE-COUNTER shall be qualified if more
      *> than one file description entry containing a LINAGE clause has
      *> been specified in the source element." F1 and F2 both carry a
      *> LINAGE clause in this source element; the MOVE names
      *> LINAGE-COUNTER with no file-name and is refused at that line.
      *> The accepted, qualified half is g2-qual-linage-counter-two-fds.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2QUAL07.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "G2QL6B1.TXT".
           SELECT F2 ASSIGN TO "G2QL6B2.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  F1 LINAGE IS 10 LINES.
       01  R1 PIC X(10).
       FD  F2 LINAGE IS 20 LINES.
       01  R2 PIC X(10).
       WORKING-STORAGE SECTION.
       01  N PIC 99.
       PROCEDURE DIVISION.
           OPEN OUTPUT F1 F2.
           MOVE LINAGE-COUNTER TO N.
           CLOSE F1 F2.
           STOP RUN.
