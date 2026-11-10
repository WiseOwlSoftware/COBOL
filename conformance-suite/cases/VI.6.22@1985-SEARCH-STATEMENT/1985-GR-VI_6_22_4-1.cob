      *>--- conformance-case
      *> id: 1985-GR-VI_6_22_4-1
      *> rules: [1985-GR-VI.6.22.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.22.4 GR1 (THE SEARCH STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SRCHT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TBL VALUE "ABC".
           05  ENT PIC X OCCURS 3 TIMES INDEXED BY IX.
       01  FLG PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       P1.
           SET IX TO 1.
           SEARCH ENT AT END DISPLAY "NONE A"
               WHEN ENT (IX) = "Z" DISPLAY "FOUND Z"
           END-SEARCH
           DISPLAY "AFTER A".
           SET IX TO 1.
           SEARCH ENT AT END DISPLAY "NONE B"
               WHEN ENT (IX) = "Z" DISPLAY "FOUND Z".
           DISPLAY "AFTER B".
           SET IX TO 1.
           IF FLG = 1
               SEARCH ENT AT END DISPLAY "NONE C"
                   WHEN ENT (IX) = "Z" DISPLAY "FOUND Z"
           ELSE
               DISPLAY "ELSE"
           END-IF
           DISPLAY "AFTER C".
           SET IX TO 1.
           IF FLG = 1
               SEARCH ENT
                   WHEN ENT (IX) = "Z" DISPLAY "FOUND Z"
           END-IF
           DISPLAY "AFTER D".
           STOP RUN.
