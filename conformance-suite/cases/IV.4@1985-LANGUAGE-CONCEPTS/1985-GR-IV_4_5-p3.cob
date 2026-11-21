      *>--- conformance-case
      *> id: 1985-GR-IV_4_5-p3
      *> rules: [1985-GR-IV.4.5-p3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.5 p3 (LANGUAGE CONCEPTS). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-06.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-GR-IV.4.5-p3: the implementor defines the scope of an
      * external switch. WiseOwl documents RUN UNIT scope (DOC-A.1-191):
      * a SET in PROGA is seen by the separately compiled PROGB.
      * Expect: A=OFF / B=ON / A=ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PROGA.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SWITCH-3 IS SWA ON STATUS IS SWA-ON OFF STATUS IS SWA-OFF.
       PROCEDURE DIVISION.
       P0.
           IF SWA-OFF DISPLAY "A=OFF" ELSE DISPLAY "A=ON".
           SET SWA TO ON.
           CALL "PROGB".
           IF SWA-ON DISPLAY "A=ON" ELSE DISPLAY "A=OFF".
           STOP RUN.
       END PROGRAM PROGA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PROGB.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SWITCH-3 IS SWB ON STATUS IS SWB-ON.
       PROCEDURE DIVISION.
       P0.
           IF SWB-ON DISPLAY "B=ON" ELSE DISPLAY "B=OFF".
           EXIT PROGRAM.
       END PROGRAM PROGB.
