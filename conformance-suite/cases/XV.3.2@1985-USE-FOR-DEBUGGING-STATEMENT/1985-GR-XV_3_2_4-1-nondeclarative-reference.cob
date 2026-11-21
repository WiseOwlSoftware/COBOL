      *>--- conformance-case
      *> id: 1985-GR-XV_3_2_4-1-nondeclarative-reference
      *> rules: [1985-SR-XV.3.2.3-2]
      *> flag: {kind: syntax, at: [85]}
      *> control: 1985-GR-XV_3_2_4-1
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XV.3.2.3 SR2: "Except in the USE FOR DEBUGGING statement itself, there must be no reference to any non-declarative procedure within the debugging section." The line whose sequence number area reads
      *> << names MAIN-PARA, a non-declarative procedure, in a PERFORM
      *> within the debugging section: the violation. Its control
      *> 1985-GR-XV_3_2_4-1 performs the declarative procedure ERR-PARA there.
      *> WiseOwl compiles it and warns (COBOL2375, docs/CONFORMANCE.md
      *> D-DECLREF; kb/Work PB4995, PB5461).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DBGGR1F.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. ANY-COMPUTER WITH DEBUGGING MODE.
       OBJECT-COMPUTER. ANY-COMPUTER.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "F1".
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1 PIC X.
       WORKING-STORAGE SECTION.
       01  WATCHED PIC X VALUE SPACE.
       01  CNT     PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG-SECT SECTION.
           USE FOR DEBUGGING ON ERR-PARA MAIN-PARA WATCHED.
       DBG-PARA.
           ADD 1 TO CNT.
           DISPLAY "DEBUG " CNT " [" DEBUG-NAME "]".
           IF CNT = 1
               MOVE "X" TO WATCHED
<<             PERFORM MAIN-PARA.
       ERR-SECT SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F1.
       ERR-PARA.
           DISPLAY "ERR-PARA".
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-PARA.
           DISPLAY "MAIN " WATCHED.
           MOVE "Y" TO WATCHED.
           STOP RUN.
