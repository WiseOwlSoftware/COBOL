      *>--- conformance-case
      *> id: 1985-GR-IV_4_3_3-p1
      *> rules: [1985-GR-IV.4.3.3-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.3 p1 (CONCEPT OF COMPUTER INDEPENDENT DATA
      *> DESCRIPTION). An output test: the program must translate and
      *> run at 85 and DISPLAY exactly the lines its .out holds, the
      *> outcome the rule requires. Placed from written and run by the
      *> PB5453 reader of lineage-reads-1985/s-04.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * 1985-GR-IV.4.3.3-p1: every elementary item has a class and a
      * category; a group is treated at object time as alphanumeric
      * regardless of the class of its subordinate items.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GRPCLS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G-NUM.
           05  GN1           PIC 9(2) VALUE 11.
           05  GN2           PIC 9(2) VALUE 22.
       01  E-ALPHA           PIC A(4) VALUE "ABCD".
       01  E-NUM             PIC 9(4) VALUE 12.
       01  E-NED             PIC Z(3)9.
       01  E-AED             PIC XXBX.
       01  E-AN              PIC X(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 12 TO G-NUM.
           DISPLAY "G-NUM=[" G-NUM "]".
           MOVE E-NUM TO G-NUM.
           DISPLAY "G-NUM FROM NUM=[" G-NUM "]".
           IF E-ALPHA ALPHABETIC DISPLAY "ALPHABETIC OK".
           IF E-NUM NUMERIC DISPLAY "NUMERIC OK".
           MOVE E-NUM TO E-NED.
           DISPLAY "NED=[" E-NED "]".
           MOVE "ABC" TO E-AED.
           DISPLAY "AED=[" E-AED "]".
           MOVE E-NED TO E-AN.
           DISPLAY "AN=[" E-AN "]".
           STOP RUN.
