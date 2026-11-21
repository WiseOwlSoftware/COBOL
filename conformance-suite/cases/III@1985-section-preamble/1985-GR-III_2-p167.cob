      *>--- conformance-case
      *> id: 1985-GR-III_2-p167
      *> rules: [1985-GR-III.2-p167, 1985-GR-III.2-p168]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p167; §1985:III.2 p168 ((section preamble)). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-99.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MASS.
      * 1985 III.2 p167 mass storage: data organized and maintained
      * both sequentially and nonsequentially; p168 the MSCS directs
      * the processing of mass storage files (REWRITE replaces,
      * DELETE removes, random READ selects by key).
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "mass.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK
               FILE STATUS IS ST.
       DATA DIVISION.
       FILE SECTION.
       FD RELF.
       01 RREC PIC X(4).
       WORKING-STORAGE SECTION.
       01 RK PIC 9(4).
       01 ST PIC XX.
       01 SEQ PIC X(12) VALUE SPACES.
       01 FAILS PIC 99 VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RELF.
      * written out of order: nonsequential maintenance
           MOVE 3 TO RK. MOVE "CCCC" TO RREC.
           WRITE RREC INVALID KEY ADD 1 TO FAILS.
           MOVE 1 TO RK. MOVE "AAAA" TO RREC.
           WRITE RREC INVALID KEY ADD 1 TO FAILS.
           MOVE 2 TO RK. MOVE "BBBB" TO RREC.
           WRITE RREC INVALID KEY ADD 1 TO FAILS.
           IF ST NOT = "00" ADD 1 TO FAILS.
           CLOSE RELF.
           OPEN I-O RELF.
      * random read selects record 2 by its key
           MOVE 2 TO RK.
           READ RELF INVALID KEY ADD 1 TO FAILS.
           DISPLAY "RANDOM 2 [" RREC "] " ST.
           IF RREC NOT = "BBBB" ADD 1 TO FAILS.
      * the MSCS replaces record 1 and removes record 3
           MOVE 1 TO RK.
           READ RELF INVALID KEY ADD 1 TO FAILS.
           MOVE "XXXX" TO RREC.
           REWRITE RREC INVALID KEY ADD 1 TO FAILS.
           DISPLAY "REWRITE 1 " ST.
           MOVE 3 TO RK.
           DELETE RELF RECORD INVALID KEY ADD 1 TO FAILS.
           DISPLAY "DELETE 3 " ST.
           READ RELF INVALID KEY DISPLAY "READ 3 INVALID " ST.
           IF ST NOT = "23" ADD 1 TO FAILS.
      * sequential processing of the same file, in key order
           MOVE 1 TO RK.
           START RELF KEY IS NOT LESS THAN RK
               INVALID KEY ADD 1 TO FAILS.
           READ RELF NEXT RECORD AT END ADD 1 TO FAILS.
           MOVE RREC TO SEQ (1:4).
           READ RELF NEXT RECORD AT END ADD 1 TO FAILS.
           MOVE RREC TO SEQ (5:4).
           READ RELF NEXT RECORD AT END MOVE "EOF " TO SEQ (9:4).
           DISPLAY "SEQUENTIAL [" SEQ "]".
           IF SEQ NOT = "XXXXBBBBEOF " ADD 1 TO FAILS.
           CLOSE RELF.
           IF FAILS = 0
               DISPLAY "MASS PASS"
           ELSE
               DISPLAY "MASS FAIL " FAILS.
           STOP RUN.
