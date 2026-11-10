      *>--- conformance-case
      *> id: 1985-GR-VI_6_22_4-9
      *> rules: [1985-GR-VI.6.22.4-9]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.22.4 GR9 (THE SEARCH STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SRCHEND.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T-VALS PIC X(3) VALUE "ABC".
       01  T REDEFINES T-VALS.
           05  E PIC X OCCURS 3 TIMES INDEXED BY I.
       01  W-FLAG PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET I TO 1.
           IF W-FLAG = 1
               SEARCH E
                   AT END DISPLAY "NOT FOUND"
                   WHEN E (I) = "Z" DISPLAY "FOUND"
               END-SEARCH
               DISPLAY "AFTER"
           ELSE
               DISPLAY "ELSE"
           END-IF.
           DISPLAY "END".
           STOP RUN.
