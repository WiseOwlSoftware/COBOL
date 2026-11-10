      *>--- conformance-case
      *> id: 1985-GR-XV_3_2_4-2
      *> rules: [1985-GR-XV.3.2.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XV.3.2.4 GR2 (THE USE FOR DEBUGGING STATEMENT). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the confirmation pass's redraft (WEAK
      *> original) by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DBGFILE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. ANY-COMPUTER WITH DEBUGGING MODE.
       OBJECT-COMPUTER. ANY-COMPUTER.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT TFILE ASSIGN TO "TFILE"
               FILE STATUS IS T-STAT.
           SELECT RFILE ASSIGN TO "RFILE"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS R-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  TFILE.
       01  TREC PIC X(10).
       FD  RFILE.
       01  RREC PIC X(10).
       WORKING-STORAGE SECTION.
       01  T-STAT PIC XX.
       01  R-KEY  PIC 9(4).
       01  INV    PIC X VALUE "N".
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG-SEC SECTION.
           USE FOR DEBUGGING ON TFILE RFILE.
       DBG-PAR.
           DISPLAY "    DEBUG " DEBUG-NAME (1:5).
       ERR-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON TFILE.
       ERR-PAR.
           DISPLAY "    ERROR PROCEDURE " T-STAT.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       P1.
           DISPLAY "OPEN OUTPUT".
           OPEN OUTPUT TFILE.
           DISPLAY "WRITE".
           MOVE "RECORD1" TO TREC.
           WRITE TREC.
           DISPLAY "CLOSE".
           CLOSE TFILE.
           DISPLAY "OPEN INPUT".
           OPEN INPUT TFILE.
           DISPLAY "READ A RECORD".
           READ TFILE AT END DISPLAY "    AT END".
           DISPLAY "READ AT END".
           READ TFILE AT END DISPLAY "    AT END".
           DISPLAY "READ AFTER THE END, NO AT END PHRASE".
           READ TFILE.
           DISPLAY "CLOSE".
           CLOSE TFILE.
       P2.
           DISPLAY "RELATIVE: OPEN OUTPUT, WRITE, CLOSE, OPEN I-O".
           OPEN OUTPUT RFILE.
           MOVE 1 TO R-KEY.
           MOVE "RELATIVE1" TO RREC.
           WRITE RREC INVALID KEY MOVE "Y" TO INV.
           CLOSE RFILE.
           OPEN I-O RFILE.
           DISPLAY "START".
           START RFILE KEY IS EQUAL TO R-KEY
               INVALID KEY MOVE "Y" TO INV.
           PERFORM SHOW-INV.
           DISPLAY "START, NO SUCH RECORD".
           MOVE 5 TO R-KEY.
           START RFILE KEY IS EQUAL TO R-KEY
               INVALID KEY MOVE "Y" TO INV.
           PERFORM SHOW-INV.
           DISPLAY "READ, NO SUCH RECORD".
           READ RFILE INVALID KEY MOVE "Y" TO INV.
           PERFORM SHOW-INV.
           DISPLAY "REWRITE".
           MOVE 1 TO R-KEY.
           REWRITE RREC INVALID KEY MOVE "Y" TO INV.
           PERFORM SHOW-INV.
           DISPLAY "DELETE".
           DELETE RFILE INVALID KEY MOVE "Y" TO INV.
           PERFORM SHOW-INV.
           DISPLAY "DELETE, NO SUCH RECORD".
           DELETE RFILE INVALID KEY MOVE "Y" TO INV.
           PERFORM SHOW-INV.
           DISPLAY "CLOSE".
           CLOSE RFILE.
           STOP RUN.
       SHOW-INV.
           IF INV = "Y"
               DISPLAY "    INVALID KEY"
           ELSE
               DISPLAY "    NO INVALID KEY".
           MOVE "N" TO INV.
