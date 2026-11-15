      *>--- conformance-case
      *> id: g2-occurs-cd-record-tables
      *> rules: [2002-SR-13.16.36.2-3, 2002-SR-13.16.36.2-4, 2002-SR-13.16.36.2-5, 2002-SR-13.16.36.2-6, 2002-SR-13.16.36.2-7, 2002-GR-13.16.36.3-3, 2002-SR-13.16.36.2-9, 2002-SR-13.16.36.2-15, 2002-SR-13.16.36.2-16, 2002-SR-13.16.36.2-18]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> Tables in a record description entry under an OUTPUT CD. At 2002
      *> such records are ordinary data description entries (A.1.1.2 sends
      *> them to 13.14), so every OCCURS rule reaches them.
      *> §2002:13.16.36.2 SR3 - key TS is the subject itself; key GK is
      *>   subordinate to TG (A1/A2, B0-B2 run only if both compile).
      *> §2002:13.16.36.2 SR4 - GK sits under group GG, which has no
      *>   OCCURS clause: the permitted form.
      *> §2002:13.16.36.2 SR5 - every KEY data-name is written bare.
      *> §2002:13.16.36.2 SR6 - TS has an OCCURS clause and is a key
      *>   because it is the subject of the entry.
      *> §2002:13.16.36.2 SR7 - XS/XS2 as a subscript, in PERFORM
      *>   VARYING (D1), SEARCH VARYING (D2), SET (D3) and in relation
      *>   conditions (D4, D5).
      *> §2002:13.16.36.3 GR3 - SORT orders TG descending (B0) and TM on
      *>   MK1 then MK2 (C1); SEARCH ALL relies on that order (A1, B1,
      *>   C2) and reports no match (A2, B2). An ascending reading of
      *>   TG prints ABCD and misses B; MK2-major order prints 20 21 19.
      *> §2002:13.16.36.2 SR9 - L7 needs seven subscripts: E1-E3 place
      *>   the one Z at byte 22 of L1(2).
      *> §2002:13.16.36.2 SR15, §2002:13.16.36.2 SR16 and
      *> §2002:13.16.36.2 SR18 - TV OCCURS 0 TO 5 DEPENDING ON integer
      *>   CNT, written before the table: F1-F3 move 2, 0 and 3
      *>   occurrences (§2002:13.16.36.3 GR8, §2002:15.39.3 RV4).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OCC02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W     PIC 9.
       01 WSUM  PIC 99.
       01 WX    PIC X(5).
       COMMUNICATION SECTION.
       CD COUT FOR OUTPUT.
       01 CDREC.
          05 TS PIC 9 OCCURS 4 ASCENDING KEY IS TS INDEXED BY XS XS2.
          05 TG OCCURS 4 DESCENDING KEY IS GK INDEXED BY XG.
             10 GG.
                15 GK PIC X.
             10 GV PIC X.
          05 TM OCCURS 3 ASCENDING KEY IS MK1 MK2 INDEXED BY XM.
             10 MK1 PIC 9.
             10 MK2 PIC 9.
          05 L1 OCCURS 2.
           10 L2 OCCURS 2.
            15 L3 OCCURS 2.
             20 L4 OCCURS 2.
              25 L5 OCCURS 2.
               30 L6 OCCURS 2.
                35 L7 PIC X OCCURS 2.
          05 CNT PIC 9.
          05 TVG.
             10 TV PIC X OCCURS 0 TO 5 DEPENDING ON CNT.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 1 TO TS(1) MOVE 3 TO TS(2)
           MOVE 5 TO TS(3) MOVE 7 TO TS(4)
           SEARCH ALL TS
               AT END DISPLAY "A1 NONE"
               WHEN TS(XS) = 5
                   SET W TO XS
                   DISPLAY "A1 " W
           END-SEARCH
           SEARCH ALL TS
               AT END DISPLAY "A2 NONE"
               WHEN TS(XS) = 4
                   DISPLAY "A2 FOUND"
           END-SEARCH
           MOVE "B" TO GK(1) MOVE "R" TO GV(1)
           MOVE "D" TO GK(2) MOVE "P" TO GV(2)
           MOVE "A" TO GK(3) MOVE "S" TO GV(3)
           MOVE "C" TO GK(4) MOVE "Q" TO GV(4)
           SORT TG
           DISPLAY "B0 " GK(1) GK(2) GK(3) GK(4) " "
                   GV(1) GV(2) GV(3) GV(4)
           SEARCH ALL TG
               AT END DISPLAY "B1 NONE"
               WHEN GK(XG) = "B"
                   DISPLAY "B1 " GV(XG)
           END-SEARCH
           SEARCH ALL TG
               AT END DISPLAY "B2 NONE"
               WHEN GK(XG) = "E"
                   DISPLAY "B2 FOUND"
           END-SEARCH
           MOVE 2 TO MK1(1) MOVE 1 TO MK2(1)
           MOVE 1 TO MK1(2) MOVE 9 TO MK2(2)
           MOVE 2 TO MK1(3) MOVE 0 TO MK2(3)
           SORT TM
           DISPLAY "C1 " MK1(1) MK2(1) " " MK1(2) MK2(2) " "
                   MK1(3) MK2(3)
           SEARCH ALL TM
               AT END DISPLAY "C2 NONE"
               WHEN MK1(XM) = 2 AND MK2(XM) = 1
                   SET W TO XM
                   DISPLAY "C2 " W
           END-SEARCH
           MOVE 0 TO WSUM
           PERFORM VARYING XS FROM 1 BY 1 UNTIL XS > 4
               ADD TS(XS) TO WSUM
           END-PERFORM
           DISPLAY "D1 " WSUM
           SET XS2 TO 1
           SEARCH TS VARYING XS2
               AT END DISPLAY "D2 NONE"
               WHEN TS(XS2) = 7
                   SET W TO XS2
                   DISPLAY "D2 " W
           END-SEARCH
           SET XS TO 2
           SET XS UP BY 1
           SET W TO XS
           DISPLAY "D3 " W
           IF XS = 3
               DISPLAY "D4 EQ"
           ELSE
               DISPLAY "D4 NE"
           END-IF
           IF XS < XS2
               DISPLAY "D5 LT"
           ELSE
               DISPLAY "D5 GE"
           END-IF
           MOVE SPACES TO L1(1) L1(2)
           MOVE "Z" TO L7(2 1 2 1 2 1 2)
           DISPLAY "E1 " L7(2 1 2 1 2 1 2) "[" L7(1 2 1 2 1 2 1) "]"
           MOVE 0 TO WSUM
           INSPECT L1(1) TALLYING WSUM FOR ALL "Z"
           DISPLAY "E2 " WSUM
           DISPLAY "E3 " L1(2)(22:1)
           MOVE 5 TO CNT
           MOVE "ABCDE" TO TVG
           MOVE 2 TO CNT
           MOVE TVG TO WX
           DISPLAY "F1 [" WX "]"
           MOVE 0 TO CNT
           MOVE TVG TO WX
           DISPLAY "F2 [" WX "]"
           MOVE 3 TO CNT
           MOVE FUNCTION LENGTH(TVG) TO W
           DISPLAY "F3 " W
           STOP RUN.
