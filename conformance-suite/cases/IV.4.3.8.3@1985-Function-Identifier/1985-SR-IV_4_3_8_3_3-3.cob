      *>--- conformance-case
      *> id: 1985-SR-IV_4_3_8_3_3-3
      *> rules: [1985-SR-IV.4.3.8.3.3-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.8.3.3 SR3 (Function-Identifier). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FNIDENT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-TXT PIC X(5) VALUE "abcde".
       01  WS-OUT PIC X(5).
       01  WS-STR PIC X(10) VALUE SPACES.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE FUNCTION UPPER-CASE (WS-TXT) TO WS-OUT.
           DISPLAY WS-OUT.
           DISPLAY FUNCTION REVERSE (WS-TXT).
           IF FUNCTION UPPER-CASE (WS-TXT) = "ABCDE"
               DISPLAY "EQUAL"
           ELSE
               DISPLAY "UNEQUAL".
           STRING FUNCTION REVERSE (WS-TXT) DELIMITED BY SIZE
                  FUNCTION UPPER-CASE (WS-TXT) DELIMITED BY SIZE
               INTO WS-STR.
           DISPLAY WS-STR.
           STOP RUN.
