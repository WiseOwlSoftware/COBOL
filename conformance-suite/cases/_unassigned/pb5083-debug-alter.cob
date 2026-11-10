      *>--- conformance-case
      *> id: pb5083-debug-alter
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5083. X3.23-1985 USE FOR DEBUGGING general rule 3 b: the
      *> section for procedure-name-1 runs "Immediately after the execution
      *> of an ALTER statement which references procedure-name-1", and
      *> general rule 17 gives its register: DEBUG-LINE identifies the ALTER
      *> statement, DEBUG-NAME contains procedure-name-1 (PARA-A) and
      *> DEBUG-CONTENTS "the applicable procedure-name associated with the
      *> TO phrase of the ALTER statement" (PARA-C). The later GO TO PARA-A
      *> is the ordinary reference of rule 3 a, general rule 18: DEBUG-NAME
      *> PARA-A and no DEBUG-CONTENTS (spaces, rule 11).
      *> Expected output, in order: the ALTER trigger, AFTER ALTER, the
      *> GO TO trigger, then PARA-C (the altered GO TO). Why it can fail: an
      *> ALTER that runs no section prints AFTER ALTER first (the pre-fix
      *> output), one that runs it before the ALTER executes prints the
      *> old destination.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5083ALTR.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. ANYCOMP WITH DEBUGGING MODE.
       OBJECT-COMPUTER. ANYCOMP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-NAME PIC X(6).
       01  W-CONT PIC X(13).
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG SECTION.
           USE FOR DEBUGGING ON PARA-A.
       DBG-1.
           MOVE DEBUG-NAME TO W-NAME.
           MOVE DEBUG-CONTENTS TO W-CONT.
           DISPLAY "DEBUG [" W-NAME "] [" W-CONT "]".
       END DECLARATIVES.
       MAIN SECTION.
       M-1.
           ALTER PARA-A TO PROCEED TO PARA-C.
           DISPLAY "AFTER ALTER".
           GO TO PARA-A.
       PARA-A.
           GO TO PARA-B.
       PARA-B.
           DISPLAY "PARA-B".
           STOP RUN.
       PARA-C.
           DISPLAY "PARA-C".
           STOP RUN.
