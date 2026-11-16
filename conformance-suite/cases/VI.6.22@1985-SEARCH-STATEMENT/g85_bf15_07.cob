      *>--- conformance-case
      *> id: g85_bf15_07
      *> rules: [1985-GR-VI.6.22.4-8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.22.4 GR8 - format 1 VARYING. E1 = 1 2 3 4 5,
      *> INDEXED BY IA IB; E2 INDEXED BY JX; each search looks for 4.
      *> V2 VARYING IB (an index of E1's own INDEXED BY): "that
      *>    index-name is used for this search"; IA preset 1, IB preset
      *>    2 -> IB=04, IA unchanged 01
      *> V3 VARYING JX (another table's index): IA, the first index of
      *>    E1, is used; JX "is incremented by the same amount as,
      *>    and at the same time as" IA: IA 1 -> 4, JX 2 -> 5; IB
      *>    untouched 03
      *> V4 VARYING XD (index data item, set from IA at occurrence 2):
      *>    incremented with IA: IA 1 -> 4, XD from 2 -> 5, read back
      *>    through SET IB TO XD
      *> V5 VARYING N (integer item, not an index data item):
      *>    "incremented by the value one at the same time": IA 2 -> 4,
      *>    N 7 -> 9
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1507.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T1-VALS PIC X(5) VALUE "12345".
       01  T1 REDEFINES T1-VALS.
           05  E1 PIC 9 OCCURS 5 TIMES INDEXED BY IA IB.
       01  T2-VALS PIC X(5) VALUE "00000".
       01  T2 REDEFINES T2-VALS.
           05  E2 PIC 9 OCCURS 5 TIMES INDEXED BY JX.
       01  XD  USAGE IS INDEX.
       01  N   PIC 99 VALUE 0.
       01  KA  PIC 99 VALUE 0.
       01  KB  PIC 99 VALUE 0.
       01  KJ  PIC 99 VALUE 0.
       PROCEDURE DIVISION.
       V2-PARA.
           SET IA TO 1.
           SET IB TO 2.
           SEARCH E1 VARYING IB
               AT END DISPLAY "V2 AT END"
               WHEN E1 (IB) = 4 DISPLAY "V2 FOUND"
           END-SEARCH.
           SET KA TO IA.
           SET KB TO IB.
           DISPLAY "V2 IA=" KA " IB=" KB.
       V3-PARA.
           SET IA TO 1.
           SET IB TO 3.
           SET JX TO 2.
           SEARCH E1 VARYING JX
               AT END DISPLAY "V3 AT END"
               WHEN E1 (IA) = 4 DISPLAY "V3 FOUND"
           END-SEARCH.
           SET KA TO IA.
           SET KB TO IB.
           SET KJ TO JX.
           DISPLAY "V3 IA=" KA " IB=" KB " JX=" KJ.
       V4-PARA.
           SET IA TO 2.
           SET XD TO IA.
           SET IA TO 1.
           SEARCH E1 VARYING XD
               AT END DISPLAY "V4 AT END"
               WHEN E1 (IA) = 4 DISPLAY "V4 FOUND"
           END-SEARCH.
           SET KA TO IA.
           SET IB TO XD.
           SET KB TO IB.
           DISPLAY "V4 IA=" KA " XD=" KB.
       V5-PARA.
           MOVE 7 TO N.
           SET IA TO 2.
           SEARCH E1 VARYING N
               AT END DISPLAY "V5 AT END"
               WHEN E1 (IA) = 4 DISPLAY "V5 FOUND"
           END-SEARCH.
           SET KA TO IA.
           DISPLAY "V5 IA=" KA " N=" N.
           STOP RUN.
