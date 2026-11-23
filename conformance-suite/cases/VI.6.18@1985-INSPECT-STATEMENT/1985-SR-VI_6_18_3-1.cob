      *>--- conformance-case
      *> id: 1985-SR-VI_6_18_3-1
      *> rules: [1985-SR-VI.6.18.3-1]
      *> accept: [85]
      *> source-format: fixed
      *>---
      *> §1985:VI.6.18.3 SR1: "Identifier-1 must reference either a
      *> group item or any category of elementary item described,
      *> implicitly or explicitly, as USAGE IS DISPLAY." The legal
      *> form: INSPECT names a group item, an alphabetic item and a
      *> numeric item, each of USAGE DISPLAY, and each statement takes
      *> its effect (§1985:VI.6.18.4 GR1 onward counts and replaces
      *> by the characters of the item). Each leg fails if the
      *> compiler refuses its operand: GRP tallies two A in "ABAB",
      *> ALPHA ends "CBC", NUM holds two 1 in "121".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3178SR6181.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  GRP.
           05  G1      PIC X(2) VALUE "AB".
           05  G2      PIC X(2) VALUE "AB".
       01  ALPHA       PIC A(3) VALUE "ABA".
       01  NUM         PIC 9(3) VALUE 121.
       01  CNT1        PIC 99 VALUE 0.
       01  CNT2        PIC 99 VALUE 0.
       01  CNT1-E      PIC Z9.
       01  CNT2-E      PIC Z9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           INSPECT GRP TALLYING CNT1 FOR ALL "A".
           MOVE CNT1 TO CNT1-E.
           DISPLAY "GROUP " CNT1-E.
           INSPECT ALPHA REPLACING ALL "A" BY "C".
           DISPLAY ALPHA.
           INSPECT NUM TALLYING CNT2 FOR ALL "1".
           MOVE CNT2 TO CNT2-E.
           DISPLAY "NUMERIC " CNT2-E.
           STOP RUN.
