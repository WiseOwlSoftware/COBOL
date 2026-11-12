      *>--- conformance-case
      *> id: pb5272-exit-program-before-conditional
      *> rules: [FMT-14.9.14.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5272 - at COBOL-85 EXIT PROGRAM must be the last statement of a consecutive sequence of
      *>   IMPERATIVE statements (§1985:X.5.4.3 SR1: "it must appear as the last statement in that sequence").
      *>   A conditional statement (an IF without END-IF, READ ... AT END, ADD ... ON SIZE ERROR) is not
      *>   imperative (§1985:III.2, Conditional Statement), so it ends the imperative run and may follow EXIT PROGRAM.
      *>   SUBA is called with B = 1 and EXIT PROGRAM returns before the IF is reached; the caller shows BACK.
      *>   The imperative follower stays refused (negative pb4305-exit-program-not-last-at-85).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5272MAIN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN-P.
           CALL "PB5272SUB" USING B
           DISPLAY "BACK"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5272SUB.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LB PIC 9.
       PROCEDURE DIVISION USING LB.
       SUB-P.
           EXIT PROGRAM IF LB = 1 DISPLAY "Y".
       END PROGRAM PB5272SUB.
       END PROGRAM PB5272MAIN.
