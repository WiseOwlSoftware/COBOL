      *>--- conformance-case
      *> id: pb5310_nested_sum_counter_hides_global
      *> rules: [1985-SR-XIII.3.9.3-10, 1985-GR-X.1.3.8.2-3]
      *> accept: [85]
      *> source-format: fixed
      *>---
      *> kb/Work PB5310 review R1-1 - a sum counter named as the OCCURS
      *> DEPENDING ON object of a nested program's Working-Storage item
      *> (legal at 85, 1985:XIII.3.9.3 SR10d) resolves by the same scope
      *> rule as every name: 1985:X.1.3.8.2 GR3a "If the name is declared
      *> in program B, the object in program B is the referenced object"
      *> (cite.py: OK). The outer program's GLOBAL report RPT and the
      *> nested program's own report RPU each declare a CF FINAL sum
      *> counter KTOT, so in PB53NI the name KTOT is RPU's counter, not
      *> an ambiguity between the two.
      *> DERIVATION: PB53NI's MOVE 4 TO KTOT alters RPU's counter, so
      *> TAB's current extent is 4: MOVE "ABCDE" TO TAB moves ABCD and
      *> DISPLAY shows [ABCD]. MOVE 2 TO KTOT then gives TAB 2
      *> occurrences, whose content is AB: [AB].
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB53NO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPF ASSIGN TO "pb53no.txt".
       DATA DIVISION.
       FILE SECTION.
       FD RPF IS GLOBAL REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01 W-V PIC 9(3) VALUE 2.
       REPORT SECTION.
       RD RPT IS GLOBAL CONTROL FINAL.
       01 DL TYPE DETAIL LINE PLUS 1.
          05 COLUMN 1 PIC 9(3) SOURCE W-V.
       01 TYPE CF FINAL LINE PLUS 1.
          05 KTOT COLUMN 1 PIC 9(3) SUM W-V.
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "PB53NI".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB53NI.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPG ASSIGN TO "pb53ni.txt".
       DATA DIVISION.
       FILE SECTION.
       FD RPG REPORT IS RPU.
       WORKING-STORAGE SECTION.
       01 X-V PIC 9(3) VALUE 3.
       01 TAB.
          05 TE PIC X OCCURS 1 TO 5 DEPENDING ON KTOT.
       REPORT SECTION.
       RD RPU CONTROL FINAL.
       01 DM TYPE DETAIL LINE PLUS 1.
          05 COLUMN 1 PIC 9(3) SOURCE X-V.
       01 TYPE CF FINAL LINE PLUS 1.
          05 KTOT COLUMN 1 PIC 9(3) SUM X-V.
       PROCEDURE DIVISION.
       INNER-PARA.
           MOVE 4 TO KTOT.
           MOVE "ABCDE" TO TAB.
           DISPLAY "[" TAB "]".
           MOVE 2 TO KTOT.
           DISPLAY "[" TAB "]".
           EXIT PROGRAM.
       END PROGRAM PB53NI.
       END PROGRAM PB53NO.
