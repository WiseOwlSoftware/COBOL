      *>--- conformance-case
      *> id: pb4858-debug-register-receiving
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4858, review round 1 R1-3. X3.23-1985 USE FOR
      *> DEBUGGING general rule 11: "The contents of data items
      *> subordinate to DEBUG-ITEM are then updated, according to the
      *> following general rules, immediately before control is passed to
      *> that debugging section." The debugging facility alone stores into
      *> the register, so a MOVE to a reference-modified DEBUG-CONTENTS
      *> is refused (COBOL6361). Why it can fail: a binder that lets the
      *> reference-modified register reach a receiving position dies with
      *> an unhandled exception in the code generator instead.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4858RR.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
       DECLARATIVES.
       DB1 SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF A.
       DB1-B.
           MOVE "Z" TO DEBUG-CONTENTS (1:1).
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           DISPLAY A.
           STOP RUN.
