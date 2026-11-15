      *>--- conformance-case
      *> id: 1985-GR-VI_6_22_4-10
      *> rules: [1985-GR-VI.6.22.4-10]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.22.4 GR10 (THE SEARCH STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the witnesses-85m drafting pass (a
      *> first draft for a 1985 rule no pass had drafted, its
      *> expectation derived from the rule's text and its citations
      *> checked with cite.py) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SRCHFG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TBL.
           05  ELT PIC X OCCURS 4 TIMES INDEXED BY IX.
       01  K PIC 9 VALUE 0.
       01  N PIC 9 VALUE 0.
       01  FLAG PIC X VALUE "Y".
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "ABCD" TO TBL.
           PERFORM NOTHING-PARA VARYING IX FROM 1 BY 1 UNTIL IX > 4.
           SEARCH ELT
               AT END DISPLAY "S1 AT END"
               WHEN ELT (IX) = "A" DISPLAY "S1 A"
               WHEN ELT (IX) = "B" DISPLAY "S1 B"
           END-SEARCH.
           SET IX TO 1.
           SEARCH ELT VARYING K
               AT END DISPLAY "S2 AT END"
               WHEN ELT (IX) = "Z" DISPLAY "S2 Z"
               WHEN ELT (IX) = "C" SET N TO IX
                                   DISPLAY "S2 C " K " " N
           END-SEARCH.
           SET IX TO 2.
           SEARCH ELT
               AT END DISPLAY "S3 AT END"
               WHEN ELT (IX) = "B" DISPLAY "S3 FIRST"
               WHEN ELT (IX) = "B" DISPLAY "S3 SECOND"
           END-SEARCH.
           SET IX TO 1.
           SEARCH ELT
               AT END DISPLAY "S4 AT END"
               WHEN ELT (IX) = "Q" DISPLAY "S4 Q"
               WHEN ELT (IX) = "R" DISPLAY "S4 R"
           END-SEARCH.
           PERFORM NOTHING-PARA VARYING IX FROM 3 BY -1 UNTIL IX < 1.
           SEARCH ELT
               AT END DISPLAY "S5 AT END"
               WHEN FLAG = "Y" DISPLAY "S5 WHEN"
               WHEN FLAG = "N" DISPLAY "S5 SECOND"
           END-SEARCH.
           STOP RUN.
       NOTHING-PARA.
           EXIT.
