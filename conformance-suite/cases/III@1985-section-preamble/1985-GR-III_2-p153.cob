      *>--- conformance-case
      *> id: 1985-GR-III_2-p153
      *> rules: [1985-GR-III.2-p153]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p153 ((section preamble)). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-03.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985 III.2 p153: a letter is one of A-Z or a-z. The class
      * tests ALPHABETIC, ALPHABETIC-UPPER and ALPHABETIC-LOWER
      * accept every letter of each set and refuse a digit.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P153.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  UPS PIC X(26) VALUE "ABCDEFGHIJKLMNOPQRSTUVWXYZ".
       01  LO PIC X(26) VALUE "abcdefghijklmnopqrstuvwxyz".
       01  MX PIC X(4) VALUE "aB1c".
       01  OKS PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           IF UPS ALPHABETIC-UPPER ADD 1 TO OKS.
           IF LO ALPHABETIC-LOWER ADD 1 TO OKS.
           IF UPS ALPHABETIC AND LO ALPHABETIC ADD 1 TO OKS.
           IF MX NOT ALPHABETIC ADD 1 TO OKS.
           IF LO NOT ALPHABETIC-UPPER ADD 1 TO OKS.
           IF OKS = 5
               DISPLAY "P153 OK"
           ELSE
               DISPLAY "P153 FAIL " OKS.
           STOP RUN.
