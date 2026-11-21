      *>--- conformance-case
      *> id: 1985-GR-X_1_3_8-p1
      *> rules: [1985-GR-X.1.3.8-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.1.3.8 p1 (LANGUAGE CONCEPTS). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-15.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * s-15 probe 6: X.1.3.8 p1 at 85: a contained program reuses the
      * user-defined word of a container's GLOBAL data-name as its own
      * paragraph-name, and a container paragraph-name as its own
      * data-name; each reference is to the object its program
      * describes.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S15DW.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  SAMEW GLOBAL PIC X(5) VALUE "GDATA".
       PROCEDURE DIVISION.
       MAIN-P.
           CALL "S15DWI".
           PERFORM OPARA.
           DISPLAY "OUT-SAMEW=" SAMEW.
           STOP RUN.
       OPARA.
           DISPLAY "OUT-OPARA".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S15DWI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  OPARA       PIC X(5) VALUE "IDATA".
       PROCEDURE DIVISION.
       MAIN-P.
           PERFORM SAMEW.
           DISPLAY "IN-OPARA=" OPARA.
           EXIT PROGRAM.
       SAMEW.
           DISPLAY "IN-SAMEW-PARA".
       END PROGRAM S15DWI.
       END PROGRAM S15DW.
