      *>--- conformance-case
      *> id: 1989-GR-A_2_16_1-p1
      *> rules: [1989-GR-A.2.16.1-p1, 1989-FMT-A.2.16.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> Also the accepted witness of §1989:A.2.16.2, the general format
      *> FUNCTION INTEGER-OF-DATE ( argument-1 ): each reference below writes that
      *> form (kb/Work PB5740).
      *> §1989:A.2.16.1 p1 (THE INTEGER-OF-DATE FUNCTION). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-20.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      *> s-20 probe integer-of-date-85: INTEGER-OF-DATE value; type integer (accepted where an integer argument is required)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PINTEGE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC S9(8)V9(6).
       01 I PIC S9(9).
       01 N PIC S9(9).
       01 X PIC X(30).
       01 A1 PIC A(3) VALUE "ABC".
       01 A2 PIC A(3) VALUE "ABD".
       01 OK PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       P0.
           COMPUTE I = FUNCTION INTEGER-OF-DATE(16010101).
           IF I = 1
               DISPLAY "OK   INTEGER-OF-DATE value 1"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL INTEGER-OF-DATE value 1"
           END-IF.
           COMPUTE I = FUNCTION INTEGER-OF-DATE(20000101).
           IF I = 145732
               DISPLAY "OK   INTEGER-OF-DATE value 2"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL INTEGER-OF-DATE value 2"
           END-IF.
           COMPUTE I = FUNCTION MOD(FUNCTION INTEGER-OF-DATE(20000101)
               7).
           IF I = 6
               DISPLAY "OK   INTEGER-OF-DATE is an integer operand"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL INTEGER-OF-DATE is an integer operand"
           END-IF.
           IF OK = 1 DISPLAY "PASS" ELSE DISPLAY "FAIL".
           STOP RUN.
