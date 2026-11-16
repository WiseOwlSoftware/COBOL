      *>--- conformance-case
      *> id: pb5787-linage-counter-nearest-container-1985
      *> rules: [1985-SR-IV.4.3.8.1-L2.8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> The contained program P5787D has no LINAGE file of its own, and its
      *> containers P5787A and P5787B each declare a GLOBAL LINAGE file (AF, BF).
      *> §1985:IV.4.3.8.1 SRL2.8 qualifies LINAGE-COUNTER only "if more than one
      *> file description entry containing a LINAGE clause has been specified in
      *> the source program"; each GLOBAL file's counter is a global name
      *> (§1985:X.4.2.4 GR1) and a reference is to "The object in program A if
      *> the name is declared in program A" (§1985:X.1.3.8.2 GR3b.1), so
      *> P5787D's unqualified reference is the counter of the NEAREST
      *> container's file, BF (B's own reference is BF's too): BF is
      *> opened (1) and written three times, so both DISPLAYs show "04".
      *> AF, written once, would show "02".
      *> Fails if the resolver counts every visible LINAGE file (COBOL5997).
      *> From 2014 the visible count applies (pb5787-linage-counter-nearest-container-2014).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5787A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT AF ASSIGN TO "P5787AAF.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD AF IS GLOBAL LINAGE IS 10 LINES.
       01 AR PIC X(4).
       PROCEDURE DIVISION.
       PA.
           OPEN OUTPUT AF.
           MOVE "AAAA" TO AR. WRITE AR.
           CALL "P5787B".
           CLOSE AF.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5787B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT BF ASSIGN TO "P5787BBF.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD BF IS GLOBAL LINAGE IS 20 LINES.
       01 BR PIC X(4).
       PROCEDURE DIVISION.
       PB.
           OPEN OUTPUT BF.
           MOVE "BBBB" TO BR. WRITE BR. WRITE BR. WRITE BR.
           DISPLAY "B UNQ=" LINAGE-COUNTER.
           CALL "P5787D".
           CLOSE BF.
           EXIT PROGRAM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5787D.
       PROCEDURE DIVISION.
       PD.
           DISPLAY "D UNQ=" LINAGE-COUNTER.
           EXIT PROGRAM.
       END PROGRAM P5787D.
       END PROGRAM P5787B.
       END PROGRAM P5787A.