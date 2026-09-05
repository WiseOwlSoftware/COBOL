      *> reject-at: 85 2002 2014 2023
      *> ISO 14.9.35.2 (PDF page 740, RENDERED) gives the REWRITE statement
      *> [ retry-phrase ] and [ WITH LOCK | WITH NO LOCK ] and no IGNORING LOCK
      *> alternative; 5.2.1 admits only what the general format prints. Its WRITE twin
      *> is negative/pb331-write-ignoring-lock. Both were accepted until kb/Work PB331,
      *> because the merged record-lock grammar rule they share with READ carried
      *> READ's IGNORING LOCK into statements that never print it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB331RIG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RLF ASSIGN TO "pb331rig.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RL-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD RLF.
       01 RL-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 RL-KEY PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           OPEN I-O RLF.
           MOVE 1 TO RL-KEY.
           MOVE "AAAA" TO RL-REC.
           REWRITE RL-REC IGNORING LOCK
               INVALID KEY CONTINUE
           END-REWRITE.
           CLOSE RLF.
           STOP RUN.
