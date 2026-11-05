      *>--- conformance-case
      *> id: pb4713-debug-all-cd-name
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4713. In ALL [REFERENCES OF] identifier-1 the operand is a data item (X3.23-1985; ISO/IEC
      *> 1989:2023 8.4.2.1 requires "a statement shall contain a reference that uniquely identifies that
      *> resource"). CM-IN is declared, but as a cd-name, so the refusal says what it is declared as and never
      *> that nothing declares it. Why it can fail: the reference resolver that knows file-names but not
      *> cd-names says "'CM-IN' is not defined".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4713DBGALLCD.
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
           USE FOR DEBUGGING ON ALL CM-IN.
       DBG-BODY.
           DISPLAY DEBUG-NAME.
       END DECLARATIVES.
       MAIN SECTION.
       P1.
           STOP RUN.
