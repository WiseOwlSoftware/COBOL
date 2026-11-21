      *>--- conformance-case
      *> id: 1985-GR-IV_4_3-p1
      *> rules: [1985-GR-IV.4.3-p1, 1985-GR-IV.4.3.4-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3 p1; §1985:IV.4.3.4 p1 (LANGUAGE CONCEPTS). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-04.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-GR-IV.4.3-p1, 1985-GR-IV.4.3.4-p1: numbers are described
      * in the decimal standard data format; the USAGE clause selects
      * the radix, the value is the same.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RADIX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N-DISP            PIC 9(4) VALUE 1234.
       01  N-DISP-X REDEFINES N-DISP PIC X(4).
       01  N-COMP            PIC 9(4) COMP VALUE 1234.
       01  N-BIN             PIC 9(4) BINARY VALUE 1234.
       01  N-PACK            PIC 9(4) PACKED-DECIMAL VALUE 1234.
       01  OUT-D             PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "DISP-CHARS=" N-DISP-X.
           IF N-DISP = N-COMP AND N-DISP = N-BIN AND N-DISP = N-PACK
               DISPLAY "SAME-VALUE OK"
           ELSE
               DISPLAY "SAME-VALUE FAIL".
           ADD N-COMP N-BIN N-PACK GIVING OUT-D.
           DISPLAY "SUM=" OUT-D.
           STOP RUN.
