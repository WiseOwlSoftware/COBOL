      *>--- conformance-case
      *> id: 1989-RV-A_2_46_3-1
      *> rules: [1989-RV-A.2.46.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.46.3 RV1 (THE WHEN-COMPILED FUNCTION). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. WCTEST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WC.
           02  WC-YEAR  PIC 9(4).
           02  WC-MON   PIC 99.
           02  WC-DAY   PIC 99.
           02  WC-HOUR  PIC 99.
           02  WC-MIN   PIC 99.
           02  WC-SEC   PIC 99.
           02  WC-HUN   PIC 99.
           02  WC-SIGN  PIC X.
           02  WC-OFFH  PIC 99.
           02  WC-OFFM  PIC 99.
       01  OK-FLAG      PIC X VALUE "Y".
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE FUNCTION WHEN-COMPILED TO WC.
           IF FUNCTION LENGTH (FUNCTION WHEN-COMPILED) NOT = 21
               MOVE "N" TO OK-FLAG
           END-IF.
           IF WC-YEAR NOT NUMERIC OR WC-MON NOT NUMERIC
              OR WC-DAY NOT NUMERIC OR WC-HOUR NOT NUMERIC
              OR WC-MIN NOT NUMERIC OR WC-SEC NOT NUMERIC
              OR WC-HUN NOT NUMERIC OR WC-OFFH NOT NUMERIC
              OR WC-OFFM NOT NUMERIC
               MOVE "N" TO OK-FLAG
           ELSE
               IF WC-MON < 1 OR WC-MON > 12
                  OR WC-DAY < 1 OR WC-DAY > 31
                  OR WC-HOUR > 23 OR WC-MIN > 59
                  OR WC-SEC > 59 OR WC-OFFM > 59
                   MOVE "N" TO OK-FLAG
               END-IF
               IF WC-SIGN = "-"
                   IF WC-OFFH > 12
                       MOVE "N" TO OK-FLAG
                   END-IF
               ELSE
                   IF WC-SIGN = "+"
                       IF WC-OFFH > 13
                           MOVE "N" TO OK-FLAG
                       END-IF
                   ELSE
                       IF WC-SIGN = "0"
                           IF WC-OFFH NOT = 0 OR WC-OFFM NOT = 0
                               MOVE "N" TO OK-FLAG
                           END-IF
                       ELSE
                           MOVE "N" TO OK-FLAG
                       END-IF
                   END-IF
               END-IF
           END-IF.
           IF OK-FLAG = "Y"
               DISPLAY "WHEN-COMPILED OK"
           ELSE
               DISPLAY "WHEN-COMPILED BAD"
           END-IF.
           STOP RUN.
