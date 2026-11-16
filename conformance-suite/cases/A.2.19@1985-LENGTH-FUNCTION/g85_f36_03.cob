      *>--- conformance-case
      *> id: g85_f36_03
      *> rules: [1989-AR-A.2.19.3-2, 1989-RV-A.2.19.4-1, 1989-RV-A.2.19.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.19.3 AR2 - the DEPENDING item's contents at the
      *> time LENGTH is evaluated are used: REC is measured with
      *> CNT=3 and again after MOVE 7 TO CNT (lines 2 and 3).
      *> §1989:A.2.19.4 RV2 - a group containing a variable
      *> occurrence item is sized as a sending item
      *> (§1985:VI.5.8.4 GR3): GR3a for REC (CNT outside the group),
      *> 4 + 3*5 = 19 then 4 + 7*5 = 39; GR3b for REC2 (K inside the
      *> group), 1 + 2*6 = 13 then 1 + 5*6 = 31 (lines 4 and 5),
      *> never the maximum length (54, 31 only at K=5).
      *> §1989:A.2.19.4 RV1 - an elementary item: LENGTH (HDR) = 4
      *> (line 1). A leg fails if LENGTH uses the maximum occurrence
      *> count or a value fixed at compile time.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3603.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  CNT  PIC 99 VALUE 3.
       01  REC.
           05  HDR  PIC X(4).
           05  TBL  OCCURS 1 TO 10 TIMES DEPENDING ON CNT.
               10  E1  PIC X(2).
               10  E2  PIC 9(3).
       01  REC2.
           05  K   PIC 9 VALUE 2.
           05  T2  PIC X(6) OCCURS 1 TO 5 TIMES DEPENDING ON K.
       01  L    PIC 999.
       PROCEDURE DIVISION.
       P1.
           COMPUTE L = FUNCTION LENGTH (HDR).
           DISPLAY L.
           COMPUTE L = FUNCTION LENGTH (REC).
           DISPLAY L.
           MOVE 7 TO CNT.
           COMPUTE L = FUNCTION LENGTH (REC).
           DISPLAY L.
           COMPUTE L = FUNCTION LENGTH (REC2).
           DISPLAY L.
           MOVE 5 TO K.
           COMPUTE L = FUNCTION LENGTH (REC2).
           DISPLAY L.
           STOP RUN.
