      *>--- conformance-case
      *> id: 1985-GR-VI_2_2-p2
      *> rules: [1985-GR-VI.2.2-p2, 1985-SR-VI.2.2-p1]
      *> accept: [85]
      *> files: [ORDCPY]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.2.2 p2; §1985:VI.2.2 p1 (A COBOL SOURCE PROGRAM). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-08.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-SR-VI.2.2-p1 / GR-VI.2.2-p2: REPLACE before the first
      * division, COPY inside one, the four divisions in order, an
      * end program header, then a second program that ends at the
      * absence of more lines. Expect: ONE TWO
       REPLACE ==WORDX== BY =="ONE"==.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ORD1.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
           COPY ORDCPY.
       PROCEDURE DIVISION.
       P1.
           DISPLAY WORDX WITH NO ADVANCING.
           CALL "ORD2".
           STOP RUN.
       END PROGRAM ORD1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ORD2.
       PROCEDURE DIVISION.
       P1.
           DISPLAY " TWO".
           EXIT PROGRAM.
