      *>--- conformance-case
      *> id: 1985-GR-III_2-p83
      *> rules: [1985-GR-III.2-p83]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p83 ((section preamble)). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-02.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. EDCHR.
      * III.2 p83: the editing characters B 0 + - CR DB Z * $ , . /
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  V               PIC S9(4)V99 VALUE -1234.56.
       01  P               PIC S9(4)V99 VALUE 1234.56.
       01  E1              PIC $Z,ZZ9.99CR.
       01  E2              PIC $Z,ZZ9.99DB.
       01  E3              PIC ***9.99+.
       01  E4              PIC ---9.99.
       01  E5              PIC 99B99/990.
       PROCEDURE DIVISION.
       P1.
           MOVE V TO E1. DISPLAY "E1=" E1.
           MOVE P TO E1. DISPLAY "E1P=" E1.
           MOVE V TO E2. DISPLAY "E2=" E2.
           MOVE P TO E3. DISPLAY "E3=" E3.
           MOVE V TO E4. DISPLAY "E4=" E4.
           MOVE 1234567 TO E5. DISPLAY "E5=" E5.
           STOP RUN.
