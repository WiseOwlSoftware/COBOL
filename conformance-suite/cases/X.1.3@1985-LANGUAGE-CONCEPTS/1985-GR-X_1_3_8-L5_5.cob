      *>--- conformance-case
      *> id: 1985-GR-X_1_3_8-L5_5
      *> rules: [1985-GR-X.1.3.8-L5.5, 1985-GR-X.1.3.8-p6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.1.3.8 GRL5.5; §1985:X.1.3.8 p6 (LANGUAGE CONCEPTS). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-15.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * s-15 probe 5: program-name conventions (X.1.3.8.1) at 85. A
      * directly contained program without COMMON is reachable only
      * from its container; with COMMON, also from its siblings.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S15PN.
       PROCEDURE DIVISION.
       MAIN-P.
           CALL "S15PLAIN"
               ON OVERFLOW DISPLAY "PN-PLAIN-FROM-CONTAINER=MISSING"
           END-CALL.
           CALL "S15SIB".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S15SIB.
       PROCEDURE DIVISION.
       MAIN-P.
           CALL "S15PLAIN"
               ON OVERFLOW DISPLAY "SIB-PLAIN=NOT-REACHABLE"
           END-CALL.
           CALL "S15COMN"
               ON OVERFLOW DISPLAY "SIB-COMMON=MISSING"
           END-CALL.
           EXIT PROGRAM.
       END PROGRAM S15SIB.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S15PLAIN.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "PLAIN-RAN".
           EXIT PROGRAM.
       END PROGRAM S15PLAIN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S15COMN IS COMMON PROGRAM.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "COMMON-RAN".
           EXIT PROGRAM.
       END PROGRAM S15COMN.
       END PROGRAM S15PN.
