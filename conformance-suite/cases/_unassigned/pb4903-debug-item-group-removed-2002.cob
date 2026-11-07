      *>--- conformance-case
      *> id: pb4903-debug-item-group-removed-2002
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4903, refused at the later editions: ISO/IEC
      *> 1989:2002 deleted the USE FOR DEBUGGING facility (COBOL0902),
      *> so the program of pb4903-debug-item-group is accepted at
      *> --std 85 only.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4903R.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TB.
          02 T PIC X(2) OCCURS 3.
       01 G.
          02 GN PIC X(8) VALUE "KEEP".
          02 GS PIC 9(3) VALUE 5.
       01 W PIC X(86).
       PROCEDURE DIVISION.
       DECLARATIVES.
       DB1 SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF T.
       DB1-B.
           MOVE CORRESPONDING DEBUG-ITEM TO G.
           DISPLAY "A [" G "]".
           ADD CORRESPONDING DEBUG-ITEM TO G.
           DISPLAY "B [" G "]".
           MOVE CORRESPONDING G TO DEBUG-ITEM.
           DISPLAY "C [" DEBUG-NAME (1:8) "] [" DEBUG-SUB-1 "]".
           INITIALIZE DEBUG-ITEM REPLACING NUMERIC BY 3.
           DISPLAY "D [" DEBUG-NAME (1:8) "] [" DEBUG-SUB-1 "] ["
              DEBUG-SUB-2 "] [" DEBUG-SUB-3 "]".
           INITIALIZE DEBUG-ITEM.
           DISPLAY "E [" DEBUG-NAME (1:8) "] [" DEBUG-SUB-1 "] ["
              DEBUG-SUB-2 "] [" DEBUG-SUB-3 "]".
           IF DEBUG-SUB-1 = 0 AND DEBUG-SUB-3 = 0
              DISPLAY "ZERO"
           END-IF.
           MOVE ALL "X" TO DEBUG-ITEM.
           INITIALIZE DEBUG-ITEM.
           MOVE DEBUG-ITEM TO W.
           DISPLAY "F [" W (1:6) "] [" W (7:1) "] [" W (38:1) "] ["
              W (39:5) "]".
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           MOVE "AB" TO T (2).
           STOP RUN.
