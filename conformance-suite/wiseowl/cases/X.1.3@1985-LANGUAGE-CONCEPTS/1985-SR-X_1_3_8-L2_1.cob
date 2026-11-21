      *>--- conformance-case
      *> id: 1985-SR-X_1_3_8-L2_1
      *> rules: [1985-SR-X.1.3.8-L2.1, 1985-SR-X.1.3.8-L2.2, 1985-SR-X.1.3.8-p3]
      *> accept: [85]
      *> files: [S15LIB/S15PRC.cpy, S15LIB/S15TXT.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.1.3.8 SRL2.1; §1985:X.1.3.8 SRL2.2; §1985:X.1.3.8 p3
      *> (LANGUAGE CONCEPTS). An output test: the program must translate
      *> and run at 85 and DISPLAY exactly the lines its .out holds, the
      *> outcome the rule requires. A WiseOwl test: its library texts
      *> lie in library folders, and where a library-name's text is
      *> found is the implementor's (design section 3.1). Placed from
      *> written and run by the PB5453 reader of
      *> lineage-reads-1985/s-15.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * s-15 probe 4: library-name and text-name may be referenced
      * by any program (X.1.3.8): a COPY text-name OF library-name in
      * the outermost program and in a contained program. Run with
      * -- --copy <this directory> (library S15LIB is a subdirectory).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S15LB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
           COPY S15TXT OF S15LIB.
       01  WHO         PIC X(5) VALUE "OUTER".
       PROCEDURE DIVISION.
       MAIN-P.
           COPY S15PRC OF S15LIB.
           DISPLAY "OUT-CPY=" CPY-ITEM.
           CALL "S15LBI".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S15LBI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
           COPY S15TXT OF S15LIB.
       01  WHO         PIC X(5) VALUE "INNER".
       PROCEDURE DIVISION.
       MAIN-P.
           COPY S15PRC IN S15LIB.
           DISPLAY "IN-CPY=" CPY-ITEM.
           EXIT PROGRAM.
       END PROGRAM S15LBI.
       END PROGRAM S15LB.
