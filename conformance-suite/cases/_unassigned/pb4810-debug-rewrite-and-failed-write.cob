      *>--- conformance-case
      *> id: pb4810-debug-rewrite-and-failed-write
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4810. X3.23-1985 USE FOR DEBUGGING general rules 5 a and
      *> 6 a (docs/CONTROL_FLOW_DESIGN.md D20): the section of an item a WRITE
      *> or REWRITE references runs "immediately before the execution of that
      *> WRITE or REWRITE statement and after the execution of any implicit
      *> move resulting from the presence of the FROM phrase", whatever the
      *> I-O then does. The legs are the exception paths of the I-O:
      *> a WRITE whose key is a duplicate takes its INVALID KEY phrase
      *> (ISO/IEC 1989:2023 section 9.1.13.5 2): I-O status 22) and its section
      *> has already run; a REWRITE of a relative record runs it before the
      *> record is replaced; a WRITE to a file that is not open fails with
      *> I-O status 48 (section 9.1.13.7 8)) and its section has already run, once.
      *> Why each leg can fail: a section run after the WRITE would not run
      *> for the failed one at all or would run after INVALID2; the FROM
      *> move not yet done shows the old record in the record area.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4810RW.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RLF ASSIGN TO "pb4810rf.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK
               FILE STATUS IS RS.
           SELECT SF ASSIGN TO "pb4810sf.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS SS.
       DATA DIVISION.
       FILE SECTION.
       FD  RLF.
       01  RLF-REC PIC X(4).
       FD  SF.
       01  SF-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01  RK    PIC 9 VALUE 1.
       01  RS    PIC XX.
       01  SS    PIC XX.
       01  SRC   PIC X(4).
       01  W-NAME PIC X(8).
       01  W-CONT PIC X(8).
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG-RLF SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF RLF-REC.
       DBG-RLF-BODY.
           MOVE DEBUG-NAME TO W-NAME.
           MOVE DEBUG-CONTENTS TO W-CONT.
           DISPLAY "SECTION " W-NAME "=" W-CONT.
       DBG-SF SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF SF-REC.
       DBG-SF-BODY.
           MOVE DEBUG-NAME TO W-NAME.
           MOVE DEBUG-CONTENTS TO W-CONT.
           DISPLAY "SECTION " W-NAME "=" W-CONT.
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           OPEN OUTPUT RLF.
           MOVE "ONE " TO SRC.
           WRITE RLF-REC FROM SRC
               INVALID KEY DISPLAY "INVALID1"
           END-WRITE.
           DISPLAY "STATUS " RS.
           WRITE RLF-REC FROM SRC
               INVALID KEY DISPLAY "INVALID2"
           END-WRITE.
           DISPLAY "STATUS " RS.
           CLOSE RLF.
           OPEN I-O RLF.
           MOVE 1 TO RK.
           MOVE "TWO " TO SRC.
           REWRITE RLF-REC FROM SRC
               INVALID KEY DISPLAY "INVALID3"
           END-REWRITE.
           DISPLAY "STATUS " RS.
           CLOSE RLF.
           WRITE SF-REC FROM SRC.
           DISPLAY "STATUS " SS.
           STOP RUN.
