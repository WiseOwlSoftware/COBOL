      *>--- conformance-case
      *> id: 1985-FMT-VII_4_7_2-function-advancing-operand
      *> rules: [1985-FMT-VII.4.7.2, 1985-SR-IV.4.3.8.3.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> A refusal at 85. §1985:VII.4.7.2 writes the ADVANCING count
      *> as identifier-2 or integer-1, and §1985:IV.4.3.8.3.3 SR4
      *> says a numeric or integer function "may be used only in an
      *> arithmetic expression" (both cite.py --check OK). The program
      *> writes FUNCTION INTEGER (1.5) as identifier-2, so WiseOwl
      *> refuses it at 85 with COBOL0900 at the function; a compiler
      *> that accepts it fails (registrar re-read, 2026-11-15; the
      *> landed fix is kb/Work PB5108).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RR85W3.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "rr85w3.prt"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  PRTF LINAGE IS 3 LINES.
       01  PR PIC X(10).
       WORKING-STORAGE SECTION.
       01  N PIC 9 VALUE 1.
       01  I PIC 9 VALUE 0.
       01  L PIC X(4) VALUE "LINE".
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT PRTF
           PERFORM 4 TIMES
             ADD 1 TO I
             WRITE PR FROM L AFTER ADVANCING FUNCTION INTEGER (1.5)
               LINES
             END-WRITE
           END-PERFORM
           CLOSE PRTF
           STOP RUN.
