      *>--- conformance-case
      *> id: pb4811-debug-file-name-repeated
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4811. X3.23-1985 USE FOR DEBUGGING syntax rule 6: "Any
      *> given identifier, cd-name, file-name, or procedure-name may appear in
      *> only one USE FOR DEBUGGING statement and may appear only once in that
      *> statement." F is named by two USE FOR DEBUGGING statements, so the
      *> program is refused (COBOL6335). Why it can fail: a binder that keeps
      *> both targets runs two sections for one OPEN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4811RP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb4811-rep.dat".
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
       DB2 SECTION.
           USE FOR DEBUGGING ON F.
       DB2-BODY.
           DISPLAY DEBUG-NAME.
       END DECLARATIVES.
       MAIN SECTION.
       P1.
           OPEN OUTPUT F.
           CLOSE F.
           STOP RUN.
