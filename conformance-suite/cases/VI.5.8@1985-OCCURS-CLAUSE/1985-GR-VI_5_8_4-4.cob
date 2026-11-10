      *>--- conformance-case
      *> id: 1985-GR-VI_5_8_4-4
      *> rules: [1985-GR-VI.5.8.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.8.4 GR4 (THE OCCURS CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OCCKEY.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  N PIC 9.
       01  T-TAB.
           05  T-ENT OCCURS 7 TIMES
                   ASCENDING KEY IS T-K1
                   DESCENDING KEY IS T-K2
                   INDEXED BY IX.
               10  T-K1 PIC X.
               10  T-K2 PIC S9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "A" TO T-K1 (1) T-K1 (2) T-K1 (3).
           MOVE "B" TO T-K1 (4) T-K1 (5) T-K1 (6).
           MOVE "C" TO T-K1 (7).
           MOVE 1 TO T-K2 (1) T-K2 (4) T-K2 (7).
           MOVE 0 TO T-K2 (2) T-K2 (5).
           MOVE -1 TO T-K2 (3) T-K2 (6).
           SEARCH ALL T-ENT
               AT END
                   DISPLAY "NOT FOUND"
               WHEN T-K1 (IX) = "A" AND T-K2 (IX) = 1
                   SET N TO IX
                   DISPLAY "FOUND " N
           END-SEARCH.
           SEARCH ALL T-ENT
               AT END
                   DISPLAY "NOT FOUND"
               WHEN T-K1 (IX) = "A" AND T-K2 (IX) = -1
                   SET N TO IX
                   DISPLAY "FOUND " N
           END-SEARCH.
           SEARCH ALL T-ENT
               AT END
                   DISPLAY "NOT FOUND"
               WHEN T-K1 (IX) = "B" AND T-K2 (IX) = -1
                   SET N TO IX
                   DISPLAY "FOUND " N
           END-SEARCH.
           SEARCH ALL T-ENT
               AT END
                   DISPLAY "NOT FOUND"
               WHEN T-K1 (IX) = "C" AND T-K2 (IX) = 1
                   SET N TO IX
                   DISPLAY "FOUND " N
           END-SEARCH.
           SEARCH ALL T-ENT
               AT END
                   DISPLAY "NOT FOUND"
               WHEN T-K1 (IX) = "C" AND T-K2 (IX) = 0
                   DISPLAY "FOUND"
           END-SEARCH.
           STOP RUN.
