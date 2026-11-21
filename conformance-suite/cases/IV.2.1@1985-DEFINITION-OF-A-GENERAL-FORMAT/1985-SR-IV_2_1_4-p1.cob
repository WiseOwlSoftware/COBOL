      *>--- conformance-case
      *> id: 1985-SR-IV_2_1_4-p1
      *> rules: [1985-SR-IV.2.1.4-p1, 1985-SR-IV.2.1.5-p2, 1985-SR-IV.2.1.5-p3, 1985-SR-IV.4.1-p1, 1985-SR-IV.4.2.1-p2, 1985-SR-IV.4.2.1-p3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.2.1.4 p1; §1985:IV.2.1.5 p2; §1985:IV.2.1.5 p3;
      *> §1985:IV.4.1 p1; §1985:IV.4.2.1 p2; §1985:IV.4.2.1 p3
      *> (DEFINITION OF A GENERAL FORMAT). An output test: the program
      *> must translate and run at 85 and DISPLAY exactly the lines its
      *> .out holds, the outcome the rule requires. Placed from written
      *> and run by the PB5453 reader of lineage-reads-1985/s-01.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * PB5453 pilot s-01: brackets omitted/specified, ellipsis repetition
      * of a single operand and of a bracketed group, characters outside
      * the COBOL set in literals, comment lines and comment-entries,
      * PICTURE and numeric-literal punctuation, separators in literals.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P01NOT.
       AUTHOR. A,B;C. D ~ @ # $ lower case & | ^ { } [ ].
      * comment line with ~ @ # lower case ! ? ` ^ { } [ ] \ |
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A   PIC 9(3) VALUE 1.
       01  B   PIC 9(3) VALUE 2.
       01  C   PIC 9(3) VALUE 3.
       01  E   PIC Z,ZZ9.99.
       01  F   PIC 9(3)V99.
       PROCEDURE DIVISION.
       P1.
           ADD 1 TO A.
           ADD 1 TO A ROUNDED.
           ADD 10 TO A B C.
           ADD 1.5 TO A ROUNDED B C ROUNDED.
           DISPLAY A " " B " " C.
           MOVE 1234.5 TO E.
           DISPLAY E.
           MOVE 1.25 TO F.
           DISPLAY F.
           DISPLAY "a,b;c. d ~@#{}[]|^".
           DISPLAY "X" " " "Y".
           STOP RUN.
