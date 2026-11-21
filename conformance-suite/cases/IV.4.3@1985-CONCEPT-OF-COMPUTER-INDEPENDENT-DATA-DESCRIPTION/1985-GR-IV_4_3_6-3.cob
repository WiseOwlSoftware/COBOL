      *>--- conformance-case
      *> id: 1985-GR-IV_4_3_6-3
      *> rules: [1985-GR-IV.4.3.6-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.6 GR3 (CONCEPT OF COMPUTER INDEPENDENT DATA
      *> DESCRIPTION). An output test: the program must translate and
      *> run at 85 and DISPLAY exactly the lines its .out holds, the
      *> outcome the rule requires. Placed from written and run by the
      *> PB5453 reader of lineage-reads-1985/s-04.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * 1985-GR-IV.4.3.6-3: alphanumeric, alphanumeric edited and
      * alphabetic receivers: aligned leftmost, space fill or
      * truncation on the right.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ANALGN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  AN4               PIC X(4).
       01  AB4               PIC A(4).
       01  AE5               PIC XXBXX.
       01  NUM2              PIC 99 VALUE 12.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "AB" TO AN4.
           DISPLAY "AN-SHORT=[" AN4 "]".
           MOVE "ABCDEF" TO AN4.
           DISPLAY "AN-LONG=[" AN4 "]".
           MOVE NUM2 TO AN4.
           DISPLAY "AN-FROM-NUM=[" AN4 "]".
           MOVE "XY" TO AB4.
           DISPLAY "AB-SHORT=[" AB4 "]".
           MOVE "PQRSTU" TO AB4.
           DISPLAY "AB-LONG=[" AB4 "]".
           MOVE "ABC" TO AE5.
           DISPLAY "AE-SHORT=[" AE5 "]".
           MOVE "ABCDEFG" TO AE5.
           DISPLAY "AE-LONG=[" AE5 "]".
           STOP RUN.
