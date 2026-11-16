      *>--- conformance-case
      *> id: g85_bf24_11
      *> rules: [1985-GR-VIII.4.4.4-10]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.4.4 GR10 - "When files are opened with the INPUT
      *> or I-O phrase, the file position indicator is set to 1." A
      *> relative file holding records 1 REL001, 2 REL002 and
      *> 3 REL003; a sequential READ makes available the first record
      *> at or after position 1 and moves its relative record number
      *> into the RELATIVE KEY (§1985:VIII.4.5.4 GR14), so the number
      *> is printed outright.
      *>   OPEN INPUT, two READ NEXT: R1=0001 REL001, R2=0002 REL002.
      *>   CLOSE, OPEN INPUT: R3=0001 REL001 (reset, not resumed).
      *>   CLOSE, OPEN I-O: R4=0001 REL001.
      *> RK is set to 3 before each re-OPEN: the OPEN, not the
      *> RELATIVE KEY, sets the position.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2411.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FR ASSIGN TO "g85bf2411.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK.
       DATA DIVISION.
       FILE SECTION.
       FD  FR.
       01  R-REC PIC X(6).
       WORKING-STORAGE SECTION.
       01  RK PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT FR.
           MOVE 1 TO RK.
           MOVE "REL001" TO R-REC.
           WRITE R-REC INVALID KEY DISPLAY "W1-INVALID".
           MOVE 2 TO RK.
           MOVE "REL002" TO R-REC.
           WRITE R-REC INVALID KEY DISPLAY "W2-INVALID".
           MOVE 3 TO RK.
           MOVE "REL003" TO R-REC.
           WRITE R-REC INVALID KEY DISPLAY "W3-INVALID".
           CLOSE FR.
           OPEN INPUT FR.
           READ FR NEXT RECORD AT END DISPLAY "EOF-EARLY".
           DISPLAY "R1=" RK " " R-REC.
           READ FR NEXT RECORD AT END DISPLAY "EOF-EARLY".
           DISPLAY "R2=" RK " " R-REC.
           CLOSE FR.
           MOVE 3 TO RK.
           OPEN INPUT FR.
           READ FR NEXT RECORD AT END DISPLAY "EOF-EARLY".
           DISPLAY "R3=" RK " " R-REC.
           CLOSE FR.
           MOVE 3 TO RK.
           OPEN I-O FR.
           READ FR NEXT RECORD AT END DISPLAY "EOF-EARLY".
           DISPLAY "R4=" RK " " R-REC.
           CLOSE FR.
           STOP RUN.
