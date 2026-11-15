      *>--- conformance-case
      *> id: g2-suppress-in-use-before-reporting
      *> rules: [2002-SR-14.8.41.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.41.2 SR1 - SUPPRESS written where it may appear
      *> "The SUPPRESS statement may appear only in a USE BEFORE
      *> REPORTING procedure."  cite.py: OK  §2002:14.8.41.2 SR1
      *> The rule grants a place: this program writes SUPPRESS PRINTING
      *> inside a USE BEFORE REPORTING procedure and shows it compiles
      *> and takes effect. Its effect is pinned through LINE-COUNTER:
      *>  §2002:14.8.41.3 GR3d, inhibited: "Any changes to LINE-COUNTER
      *>   associated with the report group" (cite.py OK);
      *>  §2002:8.4.2.14.3 GR3 "The initial value of LINE-COUNTER is set
      *>   to zero by execution of an INITIATE statement" (cite.py OK);
      *>  §2002:13.16.33.3 GR5c (relative LINE, report not divided into
      *>   pages): "the report group's first line number is obtained by
      *>   adding integer-2 to the current value of the report's
      *>   LINE-COUNTER" and GR6 "the report's LINE-COUNTER is set equal
      *>   to that line number" (cite.py OK both);
      *>  §2002:14.8.45.3 GR8 "The declarative is invoked just before
      *>   the named report group is produced" (cite.py OK).
      *> DERIVATION (R-1 has no PAGE clause; DET is LINE PLUS 1):
      *>  INITIATE: LC 0                              -> "LC=0"
      *>  N=1, not suppressed: declarative, then DET on line 0+1
      *>                                              -> "USE 1" "LC=1"
      *>  N=2, SUPPRESS executed: LC unchanged        -> "USE 2" "LC=1"
      *>  N=3, not suppressed: line 1+1               -> "USE 3" "LC=2"
      *> Fails if the SUPPRESS is refused here, if it is ignored (N=2
      *> would give LC=2, N=3 LC=3) or if it carries over to the next
      *> instance (N=3 would stay LC=1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SUP01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "G2SUP01.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01  WS-N   PIC 9 VALUE 0.
       01  WS-SUP PIC X VALUE "N".
       01  WS-LC  PIC 9.
       REPORT SECTION.
       RD  R-1.
       01  DET TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC 9 SOURCE WS-N.
       PROCEDURE DIVISION.
       DECLARATIVES.
       SUP SECTION.
           USE BEFORE REPORTING DET.
       SUP-P.
           DISPLAY "USE " WS-N.
           IF WS-SUP = "Y"
               SUPPRESS PRINTING
           END-IF.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE R-1.
           PERFORM SHOW-LC.
           MOVE 1 TO WS-N. MOVE "N" TO WS-SUP.
           GENERATE DET.
           PERFORM SHOW-LC.
           MOVE 2 TO WS-N. MOVE "Y" TO WS-SUP.
           GENERATE DET.
           PERFORM SHOW-LC.
           MOVE 3 TO WS-N. MOVE "N" TO WS-SUP.
           GENERATE DET.
           PERFORM SHOW-LC.
           TERMINATE R-1.
           CLOSE PRT.
           STOP RUN.
       SHOW-LC.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "LC=" WS-LC.
