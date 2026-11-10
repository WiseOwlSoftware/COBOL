      *>--- conformance-case
      *> id: 1985-GR-VI_6_22_4-2
      *> rules: [1985-GR-VI.6.22.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.22.4 GR2 (THE SEARCH STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SRCH1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TBL-INIT PIC X(5) VALUE "ABCDE".
       01  TBL REDEFINES TBL-INIT.
           05  ELT PIC X OCCURS 5 TIMES INDEXED BY IX.
       01  N PIC 9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET IX TO 3.
           SEARCH ELT AT END DISPLAY "NOT FOUND B"
               WHEN ELT (IX) = "B" DISPLAY "FOUND B".
           SET IX TO 3.
           SEARCH ELT AT END DISPLAY "NOT FOUND D"
               WHEN ELT (IX) = "D" SET N TO IX
                   DISPLAY "FOUND D AT " N.
           STOP RUN.
