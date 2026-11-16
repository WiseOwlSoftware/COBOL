      *>--- conformance-case
      *> id: 1985-GR-IV_4_4_2-L2_2
      *> rules: [1985-GR-IV.4.4.2-L2.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.4.2 GRL2.2 (EXPLICIT AND IMPLICIT SPECIFICATIONS).
      *> An output test. The second READ of P-SEC meets the at end
      *> condition with no AT END phrase, so the declarative D-SEC runs
      *> while PERFORM P-SEC is active: its statements are in that
      *> PERFORM's range (§1985:VI.6.21.4 GR11), and D3 is the last
      *> statement of a declarative section and not the PERFORM's
      *> exit, the state this rule names. What follows it is fixed by
      *> §1985:VII.4.4.4 GR10 c and §1985:VII.4.6.4 GR6: control
      *> returns to the statement after the READ, and the PERFORM ends
      *> at its own exit. A compiler that falls into E-SEC, or that
      *> ends the PERFORM at D3, prints other lines. Written by the
      *> refuter that overturned this row's derivation (kb/Work
      *> PB5791); the expected lines were derived before the run.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L22USE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT INF ASSIGN TO "L22USEF"
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  INF.
       01  IREC PIC X(4).
       WORKING-STORAGE SECTION.
       77  FS  PIC XX.
       77  CNT PIC 9 VALUE 0.
       77  DCNT PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON INF.
       D1.
           ADD 1 TO DCNT.
           IF DCNT > 2
               DISPLAY "D ENTERED TWICE, STOPPED"
               STOP RUN.
           DISPLAY "D1 STATUS " FS.
           GO TO D3.
       D2.
           DISPLAY "D2 SKIPPED".
       D3.
           DISPLAY "D3 LAST OF D".
       E-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON OUTPUT.
       E1.
           DISPLAY "E1 FELL INTO NEXT DECLARATIVE".
           STOP RUN.
       END DECLARATIVES.
       M-SEC SECTION.
       M0.
           ADD 1 TO CNT.
           IF CNT > 1
               DISPLAY "M0 REENTERED, STOPPED"
               STOP RUN.
           OPEN OUTPUT INF.
           MOVE "ABCD" TO IREC.
           WRITE IREC.
           CLOSE INF.
           OPEN INPUT INF.
           PERFORM P-SEC.
           DISPLAY "AFTER PERFORM".
           CLOSE INF.
           STOP RUN.
       P-SEC SECTION.
       P1.
           READ INF.
           DISPLAY "READ1 " FS " " IREC.
           READ INF.
           DISPLAY "AFTER READ2 " FS.
       P2.
           DISPLAY "P2 EXIT OF PERFORM".
