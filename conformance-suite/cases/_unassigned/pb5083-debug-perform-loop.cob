      *>--- conformance-case
      *> id: pb5083-debug-perform-loop
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5083. X3.23-1985 USE FOR DEBUGGING general rule 20: "If
      *> the transfer of control from the control mechanism associated with
      *> a PERFORM statement caused the debugging section associated with
      *> procedure-name-1 to be executed": DEBUG-LINE identifies the
      *> PERFORM statement, DEBUG-NAME contains procedure-name-1 and
      *> DEBUG-CONTENTS contains 'PERFORM LOOP'. The control mechanism
      *> transfers on every execution of the range (IV.4.4.2: "the control
      *> mechanism associated with that PERFORM statement and the first
      *> statement in that paragraph for each iterative execution of the
      *> paragraph"), the first included, and a PERFORM with no loop
      *> phrase transfers once. PERFORM PARA-A THRU PARA-B enters PARA-B by
      *> falling out of PARA-A, general rule 22: 'FALL THROUGH'.
      *> Expected output, in order: PERFORM PARA-A, PERFORM PARA-B 2 TIMES,
      *> PERFORM PARA-B VARYING I (two executions), then the THRU: PARA-A
      *> and PARA-B, each preceded by its trigger. Why it can fail: the
      *> first execution of the range left DEBUG-CONTENTS spaces (the
      *> pre-fix output), a loop form with its own iteration code misses
      *> one trigger.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5083PLOP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. X WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-NAME PIC X(6).
       01  WS-CONT PIC X(12).
       01  I       PIC 9.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG SECTION.
           USE FOR DEBUGGING ON PARA-A PARA-B.
       DBG-1.
           MOVE DEBUG-NAME TO WS-NAME.
           MOVE DEBUG-CONTENTS TO WS-CONT.
           DISPLAY WS-NAME " [" WS-CONT "]".
       END DECLARATIVES.
       MAIN SECTION.
       M-1.
           PERFORM PARA-A.
           PERFORM PARA-B 2 TIMES.
           PERFORM PARA-B VARYING I FROM 1 BY 1 UNTIL I > 2.
           PERFORM PARA-A THRU PARA-B.
           DISPLAY "DONE".
           STOP RUN.
       PARA-A.
           DISPLAY "IN PARA-A".
       PARA-B.
           DISPLAY "IN PARA-B".
