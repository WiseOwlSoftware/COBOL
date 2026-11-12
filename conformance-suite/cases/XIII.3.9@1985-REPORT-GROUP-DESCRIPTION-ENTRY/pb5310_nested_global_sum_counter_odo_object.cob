      *>--- conformance-case
      *> id: pb5310_nested_global_sum_counter_odo_object
      *> rules: [1985-SR-XIII.3.9.3-10, 1985-GR-X.1.3.8.2-1]
      *> accept: [85]
      *> source-format: fixed
      *>---
      *> kb/Work PB5310 review R1-1 sweep - the nested program PB53GI
      *> declares no counter KTOT, so the name is the GLOBAL report RPT's
      *> counter of the containing program (1985:X.1.3.8.2 GR1: the set
      *> of names "consists of all names which are defined in program B
      *> and all global names which are defined in program A"; cite.py:
      *> OK), and at 85 it may be an OCCURS DEPENDING ON object
      *> (XIII.3.9.3 SR10d). The extent is read from the container's
      *> counter: this compiled to C# naming a counter field the nested
      *> program does not have (CS0103) before the fix.
      *> DERIVATION: MOVE 4 TO KTOT alters RPT's counter; TAB has 4
      *> occurrences after MOVE "ABCDE" TO TAB: [ABCD]. MOVE 2 TO KTOT
      *> gives 2 occurrences: [AB].
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB53GO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPF ASSIGN TO "pb53go.txt".
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
           CALL "PB53GI".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB53GI.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPG ASSIGN TO "pb53gi.txt".
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
       PROCEDURE DIVISION.
       INNER-PARA.
           MOVE 4 TO KTOT.
           MOVE "ABCDE" TO TAB.
           DISPLAY "[" TAB "]".
           MOVE 2 TO KTOT.
           DISPLAY "[" TAB "]".
           EXIT PROGRAM.
       END PROGRAM PB53GI.
       END PROGRAM PB53GO.
