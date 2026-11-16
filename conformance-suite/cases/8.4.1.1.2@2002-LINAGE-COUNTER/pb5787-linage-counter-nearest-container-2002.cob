      *>--- conformance-case
      *> id: pb5787-linage-counter-nearest-container-2002
      *> rules: [2002-SR-8.4.1.1.2-6]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> The contained program P5788D has no LINAGE file of its own, and its
      *> containers P5788A and P5788B each declare a GLOBAL LINAGE file (AF, BF).
      *> §2002:8.4.1.1.2 SR6 qualifies LINAGE-COUNTER only "if more than one file
      *> description entry containing a LINAGE clause has been specified in the
      *> source element"; a source element excludes its contained units
      *> (§2002:4, definition 4.172), so
      *> P5788D's unqualified reference is the counter of the NEAREST
      *> container's file, BF (B's own reference is BF's too): BF is
      *> opened (1) and written three times, so both DISPLAYs show "04".
      *> AF, written once, would show "02".
      *> Fails if the resolver counts every visible LINAGE file (COBOL5997).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5788A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT AF ASSIGN TO "P5788AAF.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD AF IS GLOBAL LINAGE IS 10 LINES.
       01 AR PIC X(4).
       PROCEDURE DIVISION.
       PA.
           OPEN OUTPUT AF.
           MOVE "AAAA" TO AR. WRITE AR.
           CALL "P5788B".
           CLOSE AF.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5788B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT BF ASSIGN TO "P5788BBF.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD BF IS GLOBAL LINAGE IS 20 LINES.
       01 BR PIC X(4).
       PROCEDURE DIVISION.
       PB.
           OPEN OUTPUT BF.
           MOVE "BBBB" TO BR. WRITE BR. WRITE BR. WRITE BR.
           DISPLAY "B UNQ=" LINAGE-COUNTER.
           CALL "P5788D".
           CLOSE BF.
           EXIT PROGRAM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5788D.
       PROCEDURE DIVISION.
       PD.
           DISPLAY "D UNQ=" LINAGE-COUNTER.
           EXIT PROGRAM.
       END PROGRAM P5788D.
       END PROGRAM P5788B.
       END PROGRAM P5788A.