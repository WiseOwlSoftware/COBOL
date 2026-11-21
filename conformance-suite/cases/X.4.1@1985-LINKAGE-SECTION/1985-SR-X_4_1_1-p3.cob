      *>--- conformance-case
      *> id: 1985-SR-X_4_1_1-p3
      *> rules: [1985-SR-X.4.1.1-p3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.4.1.1 p3 (LINKAGE SECTION). An output test: the program
      *> must translate and run at 85 and DISPLAY exactly the lines its
      *> .out holds, the outcome the rule requires. Placed from written
      *> and run by the PB5453 reader of lineage-reads-1985/s-16.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. X411A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77 W-C PIC S9(4) COMP SYNC VALUE -12.
       77 W-J PIC X(4) JUSTIFIED RIGHT VALUE "AB".
       77 W-Z PIC 9(3) BLANK WHEN ZERO.
       77 W-G PIC S9(3) SIGN LEADING SEPARATE VALUE -7.
       77 W-I USAGE INDEX.
       01 W-T.
          05 W-E PIC X OCCURS 3 INDEXED BY TX.
       PROCEDURE DIVISION.
       P-1.
           MOVE ZERO TO W-Z.
           SET TX TO 2.
           SET W-I TO TX.
           CALL "X411B" USING W-C W-J W-Z W-G W-I W-T.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. X411B.
       DATA DIVISION.
       LINKAGE SECTION.
       77 L-C PIC S9(4) USAGE IS COMPUTATIONAL SYNCHRONIZED.
          88 L-C-NEG VALUE -12.
       77 L-J PIC X(4) JUSTIFIED RIGHT.
       77 L-Z PIC 9(3) BLANK WHEN ZERO.
       77 L-G PIC S9(3) SIGN IS LEADING SEPARATE CHARACTER.
       77 L-I USAGE IS INDEX.
       01 L-T.
          05 L-E PIC X OCCURS 3 INDEXED BY LX.
       PROCEDURE DIVISION USING L-C L-J L-Z L-G L-I L-T.
       P-1.
           IF L-C-NEG DISPLAY "L-C NEG".
           DISPLAY "L-J=[" L-J "]".
           DISPLAY "L-Z=[" L-Z "]".
           DISPLAY "L-G=" L-G.
           SET LX TO L-I.
           IF LX = 2 DISPLAY "L-I=2".
           EXIT PROGRAM.
       END PROGRAM X411B.
       END PROGRAM X411A.
