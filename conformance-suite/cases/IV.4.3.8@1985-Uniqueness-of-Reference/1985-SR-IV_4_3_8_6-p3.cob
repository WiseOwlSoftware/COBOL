      *>--- conformance-case
      *> id: 1985-SR-IV_4_3_8_6-p3
      *> rules: [1985-SR-IV.4.3.8.6-p3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.8.6 p3 (Uniqueness of Reference). A refusal: the
      *> rule forbids line 14 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL2270 there; a compiler that accepts it
      *> fails. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-05.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-SR-IV.4.3.8.6-p3: refused. V requires subscripting, so its
      * condition-name V-ON requires the same subscripting (line 14).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S05CNOS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G1.
           05  V       PIC X OCCURS 3 TIMES.
               88  V-ON  VALUE "Y".
       PROCEDURE DIVISION.
       P1.
           MOVE ALL "N" TO G1.
           MOVE "Y" TO V (2).
           IF V-ON
               DISPLAY "ON".
           STOP RUN.
