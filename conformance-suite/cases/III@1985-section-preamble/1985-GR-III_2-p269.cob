      *>--- conformance-case
      *> id: 1985-GR-III_2-p269
      *> rules: [1985-GR-III.2-p269]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p269 ((section preamble)). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-99.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RESRC.
      * 1985 III.2 p269 resource: a facility controlled by the operating
      * system that an executing program can use. X.5.2.4 GR3: the
      * object time resources checked for a called program's
      * availability are the implementor's; an unavailable one takes
      * ON EXCEPTION, an available one runs.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FAILS PIC 99 VALUE 0.
       01 HIT PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN.
           CALL "NOSUCHPG"
               ON EXCEPTION MOVE "Y" TO HIT.
           DISPLAY "UNAVAILABLE TAKEN " HIT.
           IF HIT NOT = "Y" ADD 1 TO FAILS.
           MOVE "N" TO HIT.
           CALL "RESSUB"
               ON EXCEPTION MOVE "Y" TO HIT.
           DISPLAY "AVAILABLE TAKEN " HIT.
           IF HIT NOT = "N" ADD 1 TO FAILS.
           IF FAILS = 0
               DISPLAY "RESRC PASS"
           ELSE
               DISPLAY "RESRC FAIL " FAILS.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RESSUB.
       PROCEDURE DIVISION.
       S1.
           DISPLAY "RESSUB RUNS".
           EXIT PROGRAM.
       END PROGRAM RESSUB.
       END PROGRAM RESRC.
