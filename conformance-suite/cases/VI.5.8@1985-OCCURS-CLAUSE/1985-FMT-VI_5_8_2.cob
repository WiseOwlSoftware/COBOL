      *>--- conformance-case
      *> id: 1985-FMT-VI_5_8_2
      *> rules: [1985-FMT-VI.5.8.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.8.2 (THE OCCURS CLAUSE). An output test: the program
      *> must translate and run at 85 and DISPLAY exactly the lines its
      *> .out holds, the outcome the rule requires. Placed from written
      *> and run by the witness-drafting pass (a rule the audit never
      *> drafted) by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OCCURS1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-FIXED.
           05  WS-CELL         PIC X(2) OCCURS 4 TIMES.
       01  WS-BARE.
           05  WS-DIGIT        PIC 9 OCCURS 3.
       01  WS-KEYED.
           05  WS-ENTRY        OCCURS 4 TIMES
                               ASCENDING KEY IS WS-CODE
                               DESCENDING KEY WS-RANK WS-SEQ
                               INDEXED BY IX-A IX-B.
               10  WS-CODE     PIC X(2).
               10  WS-RANK     PIC 9.
               10  WS-SEQ      PIC 9.
               10  WS-NAME     PIC X(3).
       01  WS-COUNT            PIC 9 VALUE 2.
       01  WS-VAR.
           05  WS-HEAD         PIC X(2) VALUE "H:".
           05  WS-ITEM         OCCURS 1 TO 5 TIMES
                               DEPENDING ON WS-COUNT
                               ASCENDING KEY IS WS-ITEM-KEY
                               INDEXED BY IX-V.
               10  WS-ITEM-KEY PIC X.
               10  WS-ITEM-VAL PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "AABBCCDD" TO WS-FIXED.
           DISPLAY WS-CELL (3).
           MOVE "123" TO WS-BARE.
           DISPLAY WS-DIGIT (2).
           MOVE "AA91ONEBB52TWOCC51TRIDD10FOR" TO WS-KEYED.
           SET IX-A TO 2.
           DISPLAY WS-NAME (IX-A).
           SET IX-B TO IX-A.
           SET IX-B UP BY 2.
           DISPLAY WS-CODE (IX-B) WS-RANK (IX-B) WS-SEQ (IX-B).
           SEARCH ALL WS-ENTRY
               AT END DISPLAY "NOT FOUND"
               WHEN WS-CODE (IX-A) = "CC"
                   DISPLAY WS-NAME (IX-A).
           MOVE 5 TO WS-COUNT.
           MOVE "H:AaBbCcDdEe" TO WS-VAR.
           DISPLAY WS-VAR "|".
           SEARCH ALL WS-ITEM
               AT END DISPLAY "NOT FOUND"
               WHEN WS-ITEM-KEY (IX-V) = "D"
                   DISPLAY WS-ITEM-VAL (IX-V).
           MOVE 2 TO WS-COUNT.
           DISPLAY WS-VAR "|".
           SET IX-V TO 2.
           DISPLAY WS-ITEM (IX-V).
           STOP RUN.
