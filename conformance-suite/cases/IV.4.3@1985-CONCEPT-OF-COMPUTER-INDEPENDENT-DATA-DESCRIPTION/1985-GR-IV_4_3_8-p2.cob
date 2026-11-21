      *>--- conformance-case
      *> id: 1985-GR-IV_4_3_8-p2
      *> rules: [1985-GR-IV.4.3.8-p2, 1985-SR-IV.4.3.8.1-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.8 p2; §1985:IV.4.3.8.1 SR4 (CONCEPT OF COMPUTER
      *> INDEPENDENT DATA DESCRIPTION). An output test: the program must
      *> translate and run at 85 and DISPLAY exactly the lines its .out
      *> holds, the outcome the rule requires. Placed from written and
      *> run by the PB5453 reader of lineage-reads-1985/s-05.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * 1985-GR-IV.4.3.8-p2, 1985-SR-IV.4.3.8.1-4: the same name in a
      * containing and a contained program; each reference resolves to
      * the resource described in the program containing it; a
      * contained program without its own X sees the GLOBAL one.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S05OUT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  X           PIC X(5) VALUE "OUTER" GLOBAL.
       PROCEDURE DIVISION.
       PA.
           DISPLAY "OUT=" X.
           CALL "S05IN1".
           CALL "S05IN2".
           GO TO PB.
       PB.
           DISPLAY "OUT-PB".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S05IN1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  X           PIC X(5) VALUE "INNER".
       PROCEDURE DIVISION.
       PA.
           DISPLAY "IN1=" X.
           GO TO PB.
       PB.
           DISPLAY "IN1-PB".
           EXIT PROGRAM.
       END PROGRAM S05IN1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S05IN2.
       PROCEDURE DIVISION.
       PA.
           DISPLAY "IN2=" X.
           EXIT PROGRAM.
       END PROGRAM S05IN2.
       END PROGRAM S05OUT.
