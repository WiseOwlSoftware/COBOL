      *>--- conformance-case
      *> id: pb5294-debug-sub-evaluated-once
      *> rules: [1985-GR-XV.3.2.4-14, 1985-GR-XV.3.2.4-24]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5294. §1985:XV.3.2.4 GR14: "the occurrence number of
      *> each level" of the reference enters DEBUG-SUB-n, and GR24 makes
      *> DEBUG-CONTENTS the content of "the data item referenced by
      *> identifier-1". A subscript is "evaluated only once as the first
      *> operation of the execution of that statement" (§1985:IV.4.3.8),
      *> so MOVE TE (SUB) TO SUB, which changes its own subscript, is a
      *> reference to TE (2), the occurrence SUB named when the statement
      *> began: DEBUG-SUB-1 is +0002 and DEBUG-CONTENTS is 4, TE (2)'s
      *> content, whatever SUB holds afterwards.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5294.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. ANYCOMP WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T.
           02  TE PIC 9 OCCURS 5 TIMES.
       01  SUB PIC 9 VALUE 2.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D2 SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF TE.
       D2P.
           DISPLAY "D2 [" DEBUG-SUB-1 "] [" DEBUG-CONTENTS (1:1) "]".
       END DECLARATIVES.
       M SECTION.
       M1.
           MOVE 4 TO TE (2).
           MOVE TE (SUB) TO SUB.
           DISPLAY "SUB=" SUB.
           STOP RUN.
