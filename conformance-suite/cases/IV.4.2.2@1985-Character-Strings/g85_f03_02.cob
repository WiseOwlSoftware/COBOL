      *>--- conformance-case
      *> id: g85_f03_02
      *> rules: [1985-SR-IV.4.2.2.1-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.1 SR2 - LENGTH, RANDOM and SUM are each a
      *> function-name; RANDOM is also the reserved word of ACCESS MODE
      *> IS RANDOM in the same program, its class decided by context.
      *> Fails if FUNCTION LENGTH, SUM or RANDOM is refused or misvalued,
      *> or if ACCESS MODE IS RANDOM beside FUNCTION RANDOM is refused.
      *> Each numeric function is used in an arithmetic expression
      *> (COMPUTE), as §1985:IV.4.3.8.3.3 SR4 requires.
      *> The 1985 text states no value for RANDOM, only its bounds, so
      *> only the bounds are displayed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0302.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RFILE ASSIGN TO "g85f0302.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS R-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  RFILE.
       01  R-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01  R-KEY PIC 9(4).
       01  W-STR PIC X(7) VALUE "ABCDEFG".
       01  W-LEN PIC 9(4).
       01  W-SUM PIC 9(4).
       01  W-RND PIC 9V9(8).
       PROCEDURE DIVISION.
       P1.
           COMPUTE W-LEN = FUNCTION LENGTH (W-STR).
           DISPLAY W-LEN.
           COMPUTE W-SUM = FUNCTION SUM (10 20 30).
           DISPLAY W-SUM.
           COMPUTE W-RND = FUNCTION RANDOM (5).
           IF W-RND NOT < 0 AND W-RND < 1
               DISPLAY "RANDOM IN RANGE"
           ELSE
               DISPLAY "RANDOM OUT OF RANGE"
           END-IF.
           OPEN OUTPUT RFILE.
           MOVE 3 TO R-KEY.
           MOVE "REC3" TO R-REC.
           WRITE R-REC INVALID KEY DISPLAY "WRITE INVALID".
           CLOSE RFILE.
           OPEN INPUT RFILE.
           MOVE SPACES TO R-REC.
           MOVE 3 TO R-KEY.
           READ RFILE INVALID KEY DISPLAY "READ INVALID".
           DISPLAY R-REC.
           CLOSE RFILE.
           STOP RUN.
