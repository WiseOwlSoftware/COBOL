      *>--- conformance-case
      *> id: 1985-GR-X_1_3_3-p1
      *> rules: [1985-GR-X.1.3.3-p1, 1985-GR-X.1.3.3-p4, 1985-GR-X.1.3.8-L5.2, 1985-GR-X.1.3.8-L5.3, 1985-GR-X.1.3.8-L5.4, 1985-GR-X.1.3.8-L5.6, 1985-SR-X.1.3.8-L4.1, 1985-SR-X.1.3.8-L4.2, 1985-SR-X.1.3.8-L4.3, 1985-SR-X.1.3.8-L4.4, 1985-SR-X.1.3.8-L4.5, 1985-SR-X.1.3.8-p5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.1.3.3 p1; §1985:X.1.3.3 p4; §1985:X.1.3.8 GRL5.2;
      *> §1985:X.1.3.8 GRL5.3; §1985:X.1.3.8 GRL5.4; §1985:X.1.3.8
      *> GRL5.6; §1985:X.1.3.8 SRL4.1; §1985:X.1.3.8 SRL4.2;
      *> §1985:X.1.3.8 SRL4.3; §1985:X.1.3.8 SRL4.4; §1985:X.1.3.8
      *> SRL4.5; §1985:X.1.3.8 p5 (LANGUAGE CONCEPTS). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-15.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * s-15 probe 1: global and local names, nested programs, at 85.
      * X.1.3.3 / X.1.3.8: a GLOBAL name (file, record, data-name,
      * condition-name, index-name) is reachable from contained
      * programs; a local name is not; identical user-defined words
      * in different programs name each program's own object;
      * Configuration Section names reach contained programs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S15OUT.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           UPSI-7 ON STATUS IS U7-ON OFF STATUS IS U7-OFF
           CONSOLE IS CON-DEV
           ALPHABET ALPH IS STANDARD-1
           SYMBOLIC CHARACTERS STAR IS 43
           CLASS HEXDIG IS "0" THRU "9" "A" THRU "F".
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT GF ASSIGN TO "S15GF.DAT" ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  GF GLOBAL.
       01  GREC.
           05  GREC-A  PIC X(3).
       WORKING-STORAGE SECTION.
       01  GD GLOBAL.
           05  GS      PIC X VALUE "Y".
               88  GC  VALUE "Y".
           05  GT      PIC X OCCURS 3 INDEXED BY GX.
       01  GR2 GLOBAL  PIC X(5) VALUE "GLOB2".
       01  DUP GLOBAL  PIC X(5) VALUE "OUTER".
       01  LD          PIC X(5) VALUE "LOCAL".
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "A" TO GT (1).
           MOVE "B" TO GT (2).
           MOVE "C" TO GT (3).
           CALL "S15IN1".
           DISPLAY "OUT-PARA:" WITH NO ADVANCING.
           PERFORM SAME-P.
           DISPLAY "OUT-GX=" GT (GX).
           OPEN INPUT GF.
           READ GF AT END DISPLAY "OUT-EOF".
           DISPLAY "OUT-GREC=" GREC-A.
           CLOSE GF.
           DISPLAY "OUT-LD=" LD.
           STOP RUN.
       SAME-P.
           DISPLAY "OUTER-SAME-P".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S15IN1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  DUP         PIC X(5) VALUE "INNER".
       01  W           PIC X VALUE "C".
       01  SRC         PIC X VALUE "Q".
       PROCEDURE DIVISION.
       MAIN-P.
           IF GC DISPLAY "IN1-GC=Y" ELSE DISPLAY "IN1-GC=N".
           DISPLAY "IN1-GS=" GS " GR2=" GR2.
           DISPLAY "IN1-DUP=" DUP.
           SET GX TO 2.
           OPEN OUTPUT GF.
           MOVE "XYZ" TO GREC-A.
           WRITE GREC.
           CLOSE GF.
           IF U7-ON DISPLAY "IN1-U7=ON" ELSE DISPLAY "IN1-U7=OFF".
           IF W IS HEXDIG DISPLAY "IN1-HEX=YES"
               ELSE DISPLAY "IN1-HEX=NO".
           DISPLAY "IN1-CON" UPON CON-DEV.
           MOVE STAR TO SRC.
           DISPLAY "IN1-STAR=" SRC.
           DISPLAY "IN1-PARA:" WITH NO ADVANCING.
           PERFORM SAME-P.
           CALL "S15IN2".
           EXIT PROGRAM.
       SAME-P.
           DISPLAY "INNER-SAME-P".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S15IN2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "S15SF.TMP".
       DATA DIVISION.
       FILE SECTION.
       SD  SF.
       01  SREC.
           05  SK      PIC X.
       WORKING-STORAGE SECTION.
       01  OUTS        PIC X(3) VALUE SPACES.
       01  N           PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "IN2-DUP=" DUP.
           IF GC DISPLAY "IN2-GC=Y" ELSE DISPLAY "IN2-GC=N".
           SORT SF ON ASCENDING KEY SK
               COLLATING SEQUENCE IS ALPH
               INPUT PROCEDURE IS IN-P
               OUTPUT PROCEDURE IS OUT-P.
           DISPLAY "IN2-SORT=" OUTS.
           EXIT PROGRAM.
       IN-P.
           MOVE "c" TO SK. RELEASE SREC.
           MOVE "A" TO SK. RELEASE SREC.
           MOVE "b" TO SK. RELEASE SREC.
       OUT-P.
           PERFORM 3 TIMES
               RETURN SF AT END DISPLAY "EOF"
               END-RETURN
               ADD 1 TO N
               MOVE SK TO OUTS (N:1)
           END-PERFORM.
       END PROGRAM S15IN2.
       END PROGRAM S15IN1.
       END PROGRAM S15OUT.
