      *>--- conformance-case
      *> id: 1985-GR-X_1_3_4-p8
      *> rules: [1985-GR-X.1.3.4-p8, 1985-GR-X.1.3.5-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.1.3.4 p8; §1985:X.1.3.5 p1 (LANGUAGE CONCEPTS). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-15.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * s-15 probe 3: common and initial programs; Linkage data, at 85.
      * X.1.3.5: a program may have none, one, or both of the common
      * and initial attributes; S15SIB reaches the common ones. X.1.3.4: Linkage Section data is
      * internal to its program; CALL USING associates it with the
      * caller's data item.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S15CI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  ARG         PIC X(4) VALUE "CALR".
       PROCEDURE DIVISION.
       MAIN-P.
           CALL "S15SIB".
           CALL "S15SIB".
           CALL "S15INIT".
           CALL "S15INIT".
           CALL "S15NONE" USING ARG.
           DISPLAY "CALLER-ARG=" ARG.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S15SIB.
       PROCEDURE DIVISION.
       MAIN-P.
           CALL "S15BOTH".
           CALL "S15COM".
           EXIT PROGRAM.
       END PROGRAM S15SIB.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S15BOTH IS COMMON INITIAL PROGRAM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N           PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           ADD 1 TO N.
           DISPLAY "BOTH-N=" N.
           EXIT PROGRAM.
       END PROGRAM S15BOTH.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S15COM IS COMMON PROGRAM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N           PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           ADD 1 TO N.
           DISPLAY "COM-N=" N.
           EXIT PROGRAM.
       END PROGRAM S15COM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S15INIT IS INITIAL PROGRAM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N           PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           ADD 1 TO N.
           DISPLAY "INIT-N=" N.
           EXIT PROGRAM.
       END PROGRAM S15INIT.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S15NONE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N           PIC 9 VALUE 0.
       LINKAGE SECTION.
       01  LK          PIC X(4).
       PROCEDURE DIVISION USING LK.
       MAIN-P.
           ADD 1 TO N.
           DISPLAY "NONE-N=" N " LK=" LK.
           MOVE "CHGD" TO LK.
           EXIT PROGRAM.
       END PROGRAM S15NONE.
       END PROGRAM S15CI.
