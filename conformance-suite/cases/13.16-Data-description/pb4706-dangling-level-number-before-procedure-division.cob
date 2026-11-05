      *>--- conformance-case
      *> id: pb4706-dangling-level-number-before-procedure-division
      *> rules: [FMT-13.16.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4706.  The 13.16.2 general format (cite.py --check 13.16.2 "level-number" -> OK) ends its entry with a
      *> separator period.  The entry here is a level-number and nothing else, its period forgotten, and the next line is the
      *> PROCEDURE DIVISION header.  PROCEDURE was read as the entry-name, so DIVISION drew COBOL1941 "for 'PROCEDURE'" and
      *> MAIN-PARA a second report; 8.3.2.1 1) keeps a reserved word out of the slot, so the fault is the one missing period.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4706A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-A PIC X.
       01
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
