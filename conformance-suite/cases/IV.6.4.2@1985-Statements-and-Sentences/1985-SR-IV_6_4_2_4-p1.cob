      *>--- conformance-case
      *> id: 1985-SR-IV_6_4_2_4-p1
      *> rules: [1985-SR-IV.6.4.2.4-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.4.2.4 p1 (Statements and Sentences). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-07.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P07DS.
      * 1985 IV.6.4.2.4 / IV.4.4.4: a delimited scope statement is
      * any statement written with its explicit scope terminator;
      * it is imperative (IV.6.4.2.3.1), so each one below stands
      * in an imperative-statement position: the ON SIZE ERROR of
      * an ADD that overflows (N=9).
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RLF ASSIGN TO "P07DSR.TMP"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK.
           SELECT SRT ASSIGN TO "P07DSS.TMP".
       DATA DIVISION.
       FILE SECTION.
       FD RLF.
       01 RLF-REC PIC X(4).
       SD SRT.
       01 SRT-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 RK   PIC 9(4) VALUE 1.
       01 N    PIC 9 VALUE 9.
       01 A    PIC 99 VALUE 10.
       01 K    PIC 9 VALUE 0.
       01 S1   PIC X(4) VALUE "AB,C".
       01 S2   PIC X(4) VALUE SPACES.
       01 S3   PIC X(4) VALUE SPACES.
       01 R    PIC X(40) VALUE SPACES.
       01 TT VALUE "ABC".
          05 TE PIC X OCCURS 3 INDEXED BY IX.
       01 MSG  PIC X(10).
       COMMUNICATION SECTION.
       CD CDIN FOR INPUT.
       01 CDIN-REC PIC X(87).
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT RLF.
           ADD 1 TO N ON SIZE ERROR
             ADD 1 TO A ON SIZE ERROR MOVE "X" TO R END-ADD
             SUBTRACT 1 FROM A ON SIZE ERROR MOVE "X" TO R
             END-SUBTRACT
             MULTIPLY 1 BY A ON SIZE ERROR MOVE "X" TO R END-MULTIPLY
             DIVIDE 1 INTO A ON SIZE ERROR MOVE "X" TO R END-DIVIDE
             COMPUTE A = A ON SIZE ERROR MOVE "X" TO R END-COMPUTE
             IF A = 10 MOVE "1" TO R (1:1) END-IF
             EVALUATE A WHEN 10 MOVE "2" TO R (2:1) END-EVALUATE
             PERFORM 2 TIMES ADD 1 TO K END-PERFORM
             SET IX TO 1
             SEARCH TE AT END MOVE "X" TO R
               WHEN TE (IX) = "B" MOVE "3" TO R (3:1) END-SEARCH
             STRING S1 DELIMITED BY "," INTO S2
               ON OVERFLOW MOVE "X" TO R END-STRING
             UNSTRING S1 DELIMITED BY "," INTO S3
               ON OVERFLOW MOVE "4" TO R (4:1) END-UNSTRING
             MOVE "RRR1" TO RLF-REC
             WRITE RLF-REC INVALID KEY MOVE "X" TO R END-WRITE
             CALL "P07NONE" ON OVERFLOW MOVE "5" TO R (5:1) END-CALL
           END-ADD.
           CLOSE RLF.
           OPEN I-O RLF.
           MOVE 9 TO N.
           ADD 1 TO N ON SIZE ERROR
             READ RLF INVALID KEY MOVE "X" TO R END-READ
             REWRITE RLF-REC INVALID KEY MOVE "X" TO R END-REWRITE
             START RLF KEY IS EQUAL TO RK INVALID KEY MOVE "X" TO R
             END-START
             DELETE RLF INVALID KEY MOVE "X" TO R END-DELETE
           END-ADD.
           CLOSE RLF.
           SORT SRT ON ASCENDING KEY SRT-REC
               INPUT PROCEDURE IS IN-P
               OUTPUT PROCEDURE IS OUT-P.
           DISPLAY "R=" R(1:5) " K=" K " S2=" S2 " S3=" S3.
           STOP RUN.
       IN-P.
           MOVE "SRT1" TO SRT-REC.
           RELEASE SRT-REC.
       OUT-P.
           MOVE 9 TO N.
           ADD 1 TO N ON SIZE ERROR
             RETURN SRT AT END MOVE "X" TO R END-RETURN
             RECEIVE CDIN MESSAGE INTO MSG NO DATA CONTINUE
             END-RECEIVE
           END-ADD.
           DISPLAY "RET " SRT-REC.
