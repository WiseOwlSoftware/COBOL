      *>--- conformance-case
      *> id: g2lct04_linage_counter_format_forms
      *> rules: [2002-FMT-8.4.2.13.1, 2002-SR-8.4.2.13.2-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.4.2.13.1 - LINAGE-COUNTER [ {IN|OF} file-name-1 ]: the
      *> three forms (bare, IN, OF; printed page 100, the qualifier in
      *> brackets) are each written below.
      *> §2002:8.4.2.13.2 SR3 - "Qualification requirements for
      *> LINAGE-COUNTER are defined by 8.4.1.1, Qualification", whose
      *> §2002:8.4.1.1.2 SR6 makes qualification required only "if more
      *> than one file description entry containing a LINAGE clause has
      *> been specified in the source element". G2LCT04 specifies two, so
      *> it qualifies; G2LCT04B specifies one, so the bare form is legal.
      *> Values (§2002:13.16.32.3 GR7): OPEN OUTPUT sets the counter to
      *> one (GR7d), a WRITE without ADVANCING adds one (GR7c3), WRITE
      *> AFTER ADVANCING 2 LINES adds two (GR7c2); each file has its own
      *> counter (GR7a), so the qualifier picks the file:
      *>   F1: OPEN, 2 WRITEs -> 3   "IN-F1=03"  "OF-F1=03"
      *>   F2: OPEN, 1 WRITE  -> 2   "IN-F2=02"  "OF-F2=02"
      *>   G (G2LCT04B): OPEN, WRITE AFTER 2 -> 3   "BARE=03"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2LCT04.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "G2LCT04A.TXT".
           SELECT F2 ASSIGN TO "G2LCT04B.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  F1 LINAGE IS 10 LINES.
       01  R1 PIC X(10).
       FD  F2 LINAGE IS 20 LINES.
       01  R2 PIC X(10).
       WORKING-STORAGE SECTION.
       01  N PIC 99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT F1 F2.
           MOVE "A" TO R1.
           WRITE R1.
           MOVE "B" TO R1.
           WRITE R1.
           MOVE "C" TO R2.
           WRITE R2.
           MOVE LINAGE-COUNTER IN F1 TO N.
           DISPLAY "IN-F1=" N.
           MOVE LINAGE-COUNTER OF F1 TO N.
           DISPLAY "OF-F1=" N.
           MOVE LINAGE-COUNTER IN F2 TO N.
           DISPLAY "IN-F2=" N.
           MOVE LINAGE-COUNTER OF F2 TO N.
           DISPLAY "OF-F2=" N.
           CLOSE F1 F2.
           CALL "G2LCT04B".
           STOP RUN.
       END PROGRAM G2LCT04.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2LCT04B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT G ASSIGN TO "G2LCT04C.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  G LINAGE IS 5 LINES.
       01  GR PIC X(10).
       WORKING-STORAGE SECTION.
       01  N PIC 99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT G.
           MOVE "D" TO GR.
           WRITE GR AFTER ADVANCING 2 LINES.
           MOVE LINAGE-COUNTER TO N.
           DISPLAY "BARE=" N.
           CLOSE G.
           EXIT PROGRAM.
       END PROGRAM G2LCT04B.
