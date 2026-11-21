      *>--- conformance-case
      *> id: 1985-GR-IV_7_1-p1
      *> rules: [1985-GR-IV.7.1-p1, 1985-GR-IV.7.2.2-p3, 1985-GR-IV.7.2.4-p1, 1985-SR-IV.7.1-p2, 1985-SR-IV.7.2-p2, 1985-SR-IV.7.2-p3, 1985-SR-IV.7.2-p7, 1985-SR-IV.7.2-p8, 1985-SR-IV.7.2.1-p1, 1985-SR-IV.7.2.2-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.7.1 p1; §1985:IV.7.2.2 p3; §1985:IV.7.2.4 p1;
      *> §1985:IV.7.1 p2; §1985:IV.7.2 p2; §1985:IV.7.2 p3; §1985:IV.7.2
      *> p7; §1985:IV.7.2 p8; §1985:IV.7.2.1 p1; §1985:IV.7.2.2 p1
      *> (REFERENCE FORMAT). An output test: the program must translate
      *> and run at 85 and DISPLAY exactly the lines its .out holds, the
      *> outcome the rule requires. Placed from written and run by the
      *> PB5453 reader of lineage-reads-1985/s-07.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
ZZ0001 PROGRAM-ID. P08RF.
ab*/$%* 1985 IV.7.2: sequence area any chars, any order (7.2.1);
000050* blank, comment and continuation lines (7.2.2-7.2.4).
000050 
000050 DATA DIVISION.
       WORKING-STORAGE SECTION.
<>{}[] 01 LONG-DATA-NAME-ONE PIC X(5) VALUE "WORD1".
       01 W-SPLIT PIC X(39) VALUE "ABCDEFGHIJABCDEFGHIJABCDEFGHIJABCDEFG
       
      *COMMENT BETWEEN CONTINUED AND CONTINUATION LINES !@#
      -    "YZ".
       01 W-TRAIL PIC X(39) VALUE "AB                                   
      -    "CD".
       01 W-PIC PIC 9(
      -    3) VALUE 12
      -    3.
       PROCEDURE DIVISION.
      /PAGE-EJECT COMMENT LINE: ANY CHARS ~`^ NOT SYNTAX CHECKED IF ((
       MAIN-P.
           DIS
      -    PLAY "1 " LONG-DATA-NAME-
      -    ONE.
           DISPLAY "2 " W-SPLIT.
           DISPLAY "3 [" W-TRAIL "]".
           DISPLAY "4 " W-PIC.
           DISPLAY "5 " LONG-DATA-NAME-ONE                         W-PIC
           W-PIC.
           STOP RUN.
