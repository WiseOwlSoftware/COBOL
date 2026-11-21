      *>--- conformance-case
      *> id: 1985-GR-IV_4_4_4-T1_1
      *> rules: [1985-GR-IV.4.4.4-T1.1, 1985-GR-IV.4.4.4-T1.2, 1985-GR-IV.4.4.4-T1.3, 1985-GR-IV.4.4.4-T1.4, 1985-GR-IV.4.4.4-T1.5, 1985-GR-IV.4.4.4-T1.6, 1985-GR-IV.4.4.4-T1.7, 1985-GR-IV.4.4.4-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.4.4 Table 1 row 1; §1985:IV.4.4.4 Table 1 row 2;
      *> §1985:IV.4.4.4 Table 1 row 3; §1985:IV.4.4.4 Table 1 row 4;
      *> §1985:IV.4.4.4 Table 1 row 5; §1985:IV.4.4.4 Table 1 row 6;
      *> §1985:IV.4.4.4 Table 1 row 7; §1985:IV.4.4.4 p1 (EXPLICIT AND
      *> IMPLICIT SPECIFICATIONS). An output test: the program must
      *> translate and run at 85 and DISPLAY exactly the lines its .out
      *> holds, the outcome the rule requires. Placed from written and
      *> run by the PB5453 reader of lineage-reads-1985/s-05.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * 1985-GR-IV.4.4.4-p1 and -T1.1 to -T1.7: every explicit scope
      * terminator of the 1985 table delimits its statement's scope.
      * Each conditional statement sits inside IF ... END-IF with a
      * DISPLAY after its terminator; the DISPLAY runs only if the
      * terminator ended the conditional phrase (no phrase triggers).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S05TERM.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX ASSIGN TO "s05ix.dat"
               ORGANIZATION IS INDEXED ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-KEY FILE STATUS IS FS.
           SELECT SF ASSIGN TO "s05sort.tmp".
       DATA DIVISION.
       FILE SECTION.
       FD  IX.
       01  IX-REC.
           05  IX-KEY  PIC X(2).
           05  IX-VAL  PIC X(2).
       SD  SF.
       01  SF-REC      PIC X(2).
       WORKING-STORAGE SECTION.
       77  FS          PIC XX.
       77  ONE         PIC 9 VALUE 1.
       77  N           PIC 9(4) VALUE 10.
       77  S1          PIC X(6).
       77  S2          PIC X(3).
       77  S3          PIC X(3).
       77  U1          PIC X(4) VALUE "XY,Z".
       01  TB.
           05  TE      PIC X OCCURS 3 TIMES INDEXED BY TX.
       PROCEDURE DIVISION.
       MAIN SECTION.
       M1.
           MOVE "ABC" TO TB.
           IF ONE = 1
               ADD 1 TO N ON SIZE ERROR DISPLAY "ERR" END-ADD
               DISPLAY "END-ADD " N
               MULTIPLY 2 BY N ON SIZE ERROR DISPLAY "ERR"
               END-MULTIPLY
               DISPLAY "END-MULTIPLY " N
               SET TX TO 1
               SEARCH TE AT END DISPLAY "ERR"
                   WHEN TE (TX) = "B" CONTINUE
               END-SEARCH
               DISPLAY "END-SEARCH"
               CALL "S05TSUB" ON OVERFLOW DISPLAY "ERR" END-CALL
               DISPLAY "END-CALL"
               PERFORM 2 TIMES ADD 1 TO N END-PERFORM
               DISPLAY "END-PERFORM " N
               COMPUTE N = N + 1 ON SIZE ERROR DISPLAY "ERR"
               END-COMPUTE
               DISPLAY "END-COMPUTE " N
               STRING "AB" "CD" DELIMITED BY SIZE INTO S1
                   ON OVERFLOW DISPLAY "ERR" END-STRING
               DISPLAY "END-STRING " S1
               SUBTRACT 1 FROM N ON SIZE ERROR DISPLAY "ERR"
               END-SUBTRACT
               DISPLAY "END-SUBTRACT " N
               DIVIDE 2 INTO N ON SIZE ERROR DISPLAY "ERR"
               END-DIVIDE
               DISPLAY "END-DIVIDE " N
               UNSTRING U1 DELIMITED BY "," INTO S2 S3
                   ON OVERFLOW DISPLAY "ERR" END-UNSTRING
               DISPLAY "END-UNSTRING " S2 S3
               EVALUATE N WHEN 0 DISPLAY "ERR" END-EVALUATE
               DISPLAY "END-EVALUATE"
               IF N = 0 DISPLAY "ERR" END-IF
               DISPLAY "END-IF"
           END-IF.
           PERFORM FILES.
           SORT SF ON ASCENDING KEY SF-REC
               INPUT PROCEDURE IS SIN OUTPUT PROCEDURE IS SOUT.
           STOP RUN.
       FILES SECTION.
       F1.
           OPEN OUTPUT IX.
           MOVE "K1V1" TO IX-REC.
           IF ONE = 1
               WRITE IX-REC INVALID KEY DISPLAY "ERR" END-WRITE
               DISPLAY "END-WRITE " FS
           END-IF.
           CLOSE IX.
           OPEN I-O IX.
           MOVE "K1" TO IX-KEY.
           IF ONE = 1
               START IX KEY = IX-KEY INVALID KEY DISPLAY "ERR"
               END-START
               DISPLAY "END-START " FS
               READ IX NEXT AT END DISPLAY "ERR" END-READ
               DISPLAY "END-READ " IX-REC
               MOVE "V2" TO IX-VAL
               REWRITE IX-REC INVALID KEY DISPLAY "ERR" END-REWRITE
               DISPLAY "END-REWRITE " FS
               DELETE IX INVALID KEY DISPLAY "ERR" END-DELETE
               DISPLAY "END-DELETE " FS
           END-IF.
           CLOSE IX.
       SIN SECTION.
       SI1.
           MOVE "BB" TO SF-REC. RELEASE SF-REC.
           MOVE "AA" TO SF-REC. RELEASE SF-REC.
       SOUT SECTION.
       SO1.
           IF ONE = 1
               RETURN SF AT END DISPLAY "ERR" END-RETURN
               DISPLAY "END-RETURN " SF-REC
           END-IF.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S05TSUB.
       PROCEDURE DIVISION.
       T1.
           EXIT PROGRAM.
       END PROGRAM S05TSUB.
       END PROGRAM S05TERM.
