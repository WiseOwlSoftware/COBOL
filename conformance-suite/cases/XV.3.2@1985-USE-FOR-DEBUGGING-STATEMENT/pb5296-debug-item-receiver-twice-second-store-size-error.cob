      *>--- conformance-case
      *> id: pb5296-debug-item-receiver-twice-second-store-size-error
      *> rules: [1985-GR-XV.3.2.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5296 (review R1-2). §1985:XV.3.2.4 GR6: the debugging
      *> section for A runs after a statement that causes A to be changed.
      *> ADD 4 TO A A stores 8 by the first receiver (A 4 -> 8, an item of
      *> PIC 9 holds 8); the second store is 12, a size error that leaves A
      *> alone (§1985:VI.6.4.2). A was changed by the statement, so D2 runs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5296T.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. ANYCOMP WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC 9 VALUE 4.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D2 SECTION.
           USE FOR DEBUGGING ON A.
       D2-P.
           DISPLAY "D2 " DEBUG-CONTENTS (1:1).
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-PARA.
           ADD 4 TO A A ON SIZE ERROR DISPLAY "SIZE".
           DISPLAY "A=" A.
           STOP RUN.
