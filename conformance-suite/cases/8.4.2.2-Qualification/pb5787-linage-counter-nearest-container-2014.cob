      *>--- conformance-case
      *> id: pb5787-linage-counter-nearest-container-2014
      *> rules: [SR-8.4.2.2.3-8]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> The contained program P5789D has no LINAGE file of its own, and its
      *> containers P5789A and P5789B each declare a GLOBAL LINAGE file.
      *> §2014:8.4.1.1.2 SR8 (§2023:8.4.2.2.3 SR8): the register "may be referenced within
      *> the source element in which the reference to LINAGE-COUNTER occurs", so
      *> both containers' GLOBAL files are visible and two make an unqualified
      *> reference ambiguous: refused from 2014 (COBOL5997), unlike at 85 and 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5789A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT AF ASSIGN TO "P5789AAF.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD AF IS GLOBAL LINAGE IS 10 LINES.
       01 AR PIC X(4).
       PROCEDURE DIVISION.
       PA.
           OPEN OUTPUT AF.
           MOVE "AAAA" TO AR. WRITE AR.
           CALL "P5789B".
           CLOSE AF.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5789B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT BF ASSIGN TO "P5789BBF.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD BF IS GLOBAL LINAGE IS 20 LINES.
       01 BR PIC X(4).
       PROCEDURE DIVISION.
       PB.
           OPEN OUTPUT BF.
           MOVE "BBBB" TO BR. WRITE BR. WRITE BR. WRITE BR.
           DISPLAY "B UNQ=" LINAGE-COUNTER.
           CALL "P5789D".
           CLOSE BF.
           EXIT PROGRAM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5789D.
       PROCEDURE DIVISION.
       PD.
           DISPLAY "D UNQ=" LINAGE-COUNTER.
           EXIT PROGRAM.
       END PROGRAM P5789D.
       END PROGRAM P5789B.
       END PROGRAM P5789A.