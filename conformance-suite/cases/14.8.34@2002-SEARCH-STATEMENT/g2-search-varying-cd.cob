      *>--- conformance-case
      *> id: g2-search-varying-cd
      *> rules: [2002-SR-14.8.34.2-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.34.2 SR3 - "Identifier-2 shall reference a data
      *> item whose usage is index or a data item that is an integer."
      *> Here identifier-1 and identifier-2 sit in a record under an
      *> output CD (§2002:A.1.2.2 SR9 "Record description entries
      *> subordinate to an output CD entry implicitly redefine the
      *> record area established by the output CD entry"), a reach the
      *> 2023 text has no CD for. The CD facility is obsolete (COBOL0903
      *> warning, not an error).
      *> §2002:14.8.34.3 GR3b2: an integer identifier-2 "is incremented
      *> by the value one at the same time as the search index is
      *> incremented"; GR3b1: an index data item "is incremented by the
      *> same amount as, and at the same time as, the search index".
      *>   CD table A,B,C, CN = 1, CI = 1: B at 2 -> C-FOUND B CN=02.
      *>   WS table X,Y,Z, CD CN = 1: Z at 3 -> W-FOUND Z CN=03.
      *>   CD table, CD index data item CX set from CI = 1: C at 3,
      *>   CX followed the search index -> X-FOUND C CX=3.
      *> Fails if a CD-record identifier-2 is refused, or is not stepped
      *> with the search index.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SRC02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WTAB.
          05 WE OCCURS 3 TIMES INDEXED BY WI.
             10 WV PIC X.
       01 N PIC 9.
       COMMUNICATION SECTION.
       CD COUT FOR OUTPUT.
       01 CREC.
          05 CN PIC 99.
          05 CX USAGE INDEX.
          05 CTAB OCCURS 3 TIMES INDEXED BY CI.
             10 CV PIC X.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "A" TO CV(1) MOVE "B" TO CV(2) MOVE "C" TO CV(3)
           MOVE "X" TO WV(1) MOVE "Y" TO WV(2) MOVE "Z" TO WV(3)
           SET CI TO 1
           MOVE 1 TO CN
           SEARCH CTAB VARYING CN
             AT END DISPLAY "C-NOT FOUND"
             WHEN CV(CI) = "B" DISPLAY "C-FOUND " CV(CI) " CN=" CN
           END-SEARCH
           SET WI TO 1
           MOVE 1 TO CN
           SEARCH WE VARYING CN
             AT END DISPLAY "W-NOT FOUND"
             WHEN WV(WI) = "Z" DISPLAY "W-FOUND " WV(WI) " CN=" CN
           END-SEARCH
           SET CI TO 1
           SET CX TO CI
           SEARCH CTAB VARYING CX
             AT END DISPLAY "X-NOT FOUND"
             WHEN CV(CI) = "C"
               SET CI TO CX
               SET N TO CI
               DISPLAY "X-FOUND " CV(CI) " CX=" N
           END-SEARCH
           STOP RUN.
