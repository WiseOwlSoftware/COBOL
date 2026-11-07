      *>--- conformance-case
      *> id: pb4811-debug-file-name-removed-2002
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4811. The USE FOR DEBUGGING declarative, file-name-1
      *> trigger included, is in X3.23-1985's Debug module only; ISO/IEC
      *> 1989:2002 deleted the facility, so the program is refused at the
      *> later editions (COBOL0902) and accepted at --std 85 (the
      *> pb4811-debug-file-name-triggers witness). Why it can fail: an
      *> edition gate that covers only the procedure-name and data-name
      *> arms admits a file-name arm silently above 1985.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4811R2.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb4811-rm.dat".
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 FREC PIC X(4).
       PROCEDURE DIVISION.
       DECLARATIVES.
       DB1 SECTION.
           USE FOR DEBUGGING ON F.
       DB1-BODY.
           DISPLAY DEBUG-NAME.
       END DECLARATIVES.
       MAIN SECTION.
       P1.
           OPEN OUTPUT F.
           CLOSE F.
           STOP RUN.
