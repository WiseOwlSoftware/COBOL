      *>--- conformance-case
      *> id: pb4713-debug-cd-name
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4713. The X3.23-1985 debug module triggers on the ENABLE, DISABLE, PURGE, RECEIVE and SEND of
      *> a cd-name (CCVS DB205A writes USE FOR DEBUGGING ON CM-INQUE CM-OUTQUE). CM-IN is declared by the
      *> communication description entry below, so the program is right; the cd-name trigger is not modeled, and
      *> the refusal is the staged COBOL1571 that says so. Why it can fail: a binder that asks only for a data
      *> item or a file calls the declared cd-name 'not defined' (COBOL1639), the program being wrong.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4713DBGCD.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T     PIC X(5).
       COMMUNICATION SECTION.
       CD  CM-IN FOR INPUT.
       01  IN-SPECS.
           02  IN-Q PIC X(12).
           02  FILLER PIC X(75).
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG SECTION.
           USE FOR DEBUGGING ON CM-IN.
       DBG-BODY.
           DISPLAY DEBUG-NAME.
       END DECLARATIVES.
       MAIN SECTION.
       P1.
           STOP RUN.
