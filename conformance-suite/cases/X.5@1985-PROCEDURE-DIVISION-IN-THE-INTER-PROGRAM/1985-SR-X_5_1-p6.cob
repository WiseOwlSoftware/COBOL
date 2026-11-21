      *>--- conformance-case
      *> id: 1985-SR-X_5_1-p6
      *> rules: [1985-SR-X.5.1-p6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.1 p6 (PROCEDURE DIVISION IN THE INTER-PROGRAM
      *> COMMUNICATION MODULE). An output test: the program must
      *> translate and run at 85 and DISPLAY exactly the lines its .out
      *> holds, the outcome the rule requires. Placed from written and
      *> run by the PB5453 reader of lineage-reads-1985/s-16.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. X51A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A1 PIC X VALUE "1".
       01 A2 PIC X VALUE "2".
       01 A3 PIC X VALUE "3".
       01 A4 PIC X VALUE "4".
       01 A5 PIC X VALUE "5".
       01 A6 PIC X VALUE "6".
       PROCEDURE DIVISION.
       P-1.
           CALL "X51B" USING A1 A2 A3 A4 A5 A6.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. X51B.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L1 PIC X.
       01 L2 PIC X.
       01 L3 PIC X.
       01 L4 PIC X.
       01 L5 PIC X.
       01 L6 PIC X.
       PROCEDURE DIVISION USING L1 L2 L3 L4 L5 L6.
       P-1.
           DISPLAY "ARGS=" L1 L2 L3 L4 L5 L6.
           EXIT PROGRAM.
       END PROGRAM X51B.
       END PROGRAM X51A.
