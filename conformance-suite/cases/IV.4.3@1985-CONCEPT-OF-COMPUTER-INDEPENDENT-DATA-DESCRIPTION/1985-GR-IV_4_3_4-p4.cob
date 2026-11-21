      *>--- conformance-case
      *> id: 1985-GR-IV_4_3_4-p4
      *> rules: [1985-GR-IV.4.3.4-p4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.4 p4 (CONCEPT OF COMPUTER INDEPENDENT DATA
      *> DESCRIPTION). An output test: the program must translate and
      *> run at 85 and DISPLAY exactly the lines its .out holds, the
      *> outcome the rule requires. Placed from written and run by the
      *> PB5453 reader of lineage-reads-1985/s-04.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * 1985-GR-IV.4.3.4-p4: the size of an elementary or group item
      * is its number of standard data format characters.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ITMSIZ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G.
           05  GA            PIC X(2) VALUE "AB".
           05  GN            PIC 9(3) VALUE 123.
           05  GE            PIC ZZ9.99 VALUE "  4.50".
       01  OUT-X             PIC X(14).
       01  N                 PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE N = FUNCTION LENGTH(G).
           DISPLAY "LEN-G=" N.
           COMPUTE N = FUNCTION LENGTH(GE).
           DISPLAY "LEN-GE=" N.
           MOVE G TO OUT-X.
           DISPLAY "[" OUT-X "]".
           STOP RUN.
