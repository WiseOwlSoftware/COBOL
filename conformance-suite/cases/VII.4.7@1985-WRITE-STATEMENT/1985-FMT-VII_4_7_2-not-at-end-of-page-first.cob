      *>--- conformance-case
      *> id: 1985-FMT-VII_4_7_2-not-at-end-of-page-first
      *> rules: [1985-FMT-VII.4.7.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> A refusal at 85. §1985:VII.4.7.2 prints [ AT END-OF-PAGE
      *> imperative-statement-1 ] before [ NOT AT END-OF-PAGE
      *> imperative-statement-2 ], and §1985:IV.2.1 requires the
      *> clauses "in the sequence given in the general format" (both
      *> cite.py --check OK). The program writes the NOT phrase first,
      *> so WiseOwl refuses it at 85 with COBOL0900 at its AT
      *> END-OF-PAGE; a compiler that accepts it fails. The printed
      *> order runs in 1985-FMT-VII_4_7_2-end-of-page-pair (registrar
      *> re-read, 2026-11-15).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RR85W2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "rr85w2.prt"
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
             WRITE PR FROM L AFTER ADVANCING N LINE
               NOT AT END-OF-PAGE DISPLAY "NOEOP " I
               AT END-OF-PAGE DISPLAY "EOP " I
             END-WRITE
           END-PERFORM
           CLOSE PRTF
           STOP RUN.
