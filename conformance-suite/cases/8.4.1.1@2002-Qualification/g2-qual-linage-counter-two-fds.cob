      *>--- conformance-case
      *> id: g2-qual-linage-counter-two-fds
      *> rules: [2002-SR-8.4.1.1.2-6]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.4.1.1.2 SR6 - "LINAGE-COUNTER shall be qualified if more
      *> than one file description entry containing a LINAGE clause has
      *> been specified in the source element." F1 and F2 both carry a
      *> LINAGE clause in this one source element, so every reference
      *> is qualified (Format 7, IN|OF file-name); the unqualified
      *> refusal is g2-qual-linage-counter-two-fds-unqualified, and the
      *> nested-GLOBAL leg (one own LINAGE FD, no qualifier needed) is
      *> pb5166-linage-counter-own-entries-2002.
      *> "A separate LINAGE-COUNTER is supplied for each file"
      *> (§2002:13.16.32.3 GR7a). OPEN OUTPUT sets each to one (GR7d);
      *> a WRITE without ADVANCING adds one (GR7c3), a WRITE AFTER
      *> ADVANCING 4 LINES adds four (GR7c2). The register's size is the
      *> page size's (§2002:8.4.2.13.3 GR1): two digits for 10 and 20.
      *>   F1: 1 + 1 + 1 = 3 -> "L1 03 03" (OF F1, IN F1)
      *>   F2: 1 + 4     = 5 -> "L2 05"
      *>   MOVE LINAGE-COUNTER OF F2 TO N; F2 ahead -> "L3 05 F2-AHEAD"
      *> Each leg can fail: a refused qualifier fails the compile, and a
      *> counter shared between the files or read from the wrong file
      *> prints the other count.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2QUAL06.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "G2QL6A1.TXT".
           SELECT F2 ASSIGN TO "G2QL6A2.TXT".
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
           WRITE R1 FROM "A".
           WRITE R1 FROM "B".
           WRITE R2 FROM "C" AFTER ADVANCING 4 LINES.
           DISPLAY "L1 " LINAGE-COUNTER OF F1 " " LINAGE-COUNTER IN F1.
           DISPLAY "L2 " LINAGE-COUNTER IN F2.
           MOVE LINAGE-COUNTER OF F2 TO N.
           IF LINAGE-COUNTER IN F2 > LINAGE-COUNTER OF F1
               DISPLAY "L3 " N " F2-AHEAD"
           ELSE
               DISPLAY "L3 " N " F1-AHEAD"
           END-IF.
           CLOSE F1 F2.
           STOP RUN.
