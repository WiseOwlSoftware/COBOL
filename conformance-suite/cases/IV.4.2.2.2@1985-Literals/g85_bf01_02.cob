      *>--- conformance-case
      *> id: g85_bf01_02
      *> rules: [1985-SR-IV.4.2.2.2.2-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.2.2 SR3 - one decimal point, an assumed decimal
      *> point, anywhere but rightmost; no point means an integer; and a
      *> literal formed as a numeric literal but enclosed in quotation
      *> marks is a nonnumeric literal.
      *> PT-1..PT-3: the point leftmost, inside, and absent; each value
      *> lands at the receiving item's decimal point (fails if the point
      *> is not taken as the assumed decimal point, or if a literal with
      *> no point is not read as an integer).
      *> INT-1: an integer literal is a legal operand against an
      *> alphanumeric item (§1985:VI.6.3.1.1.2 admits only an integer);
      *> fails if the literal is not taken as an integer or is compared
      *> algebraically.
      *> Q-1..Q-4: "12.5" and "12" are nonnumeric: moved to an
      *> alphanumeric item they keep the period as a character, and a
      *> numeric item compares to "12" character by character (fails if
      *> the quoted digits are treated as a numeric literal).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0102.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-E1               PIC 9.99.
       01  WS-E2               PIC 99.99.
       01  WS-E3               PIC 999.99.
       01  WS-A2               PIC XX    VALUE "12".
       01  WS-X6               PIC X(6).
       01  WS-N4               PIC 9(4)  VALUE 12.
       01  WS-CNT              PIC 99    VALUE 0.
       PROCEDURE DIVISION.
       P-MAIN.
           MOVE .5 TO WS-E1.
           DISPLAY "PT-1 " WS-E1.
           MOVE 12.5 TO WS-E2.
           DISPLAY "PT-2 " WS-E2.
           MOVE 123 TO WS-E3.
           DISPLAY "PT-3 " WS-E3.
      * INT-1: 12 treated as moved to a 2-character alphanumeric item.
           IF WS-A2 = 12
               DISPLAY "INT-1 EQ"
           ELSE
               DISPLAY "INT-1 NE".
           MOVE "12.5" TO WS-X6.
           DISPLAY "Q-1 [" WS-X6 "]".
           INSPECT WS-X6 TALLYING WS-CNT FOR ALL ".".
           DISPLAY "Q-2 " WS-CNT.
      * Q-3: "0012" against "12  ": '0' differs from '1', NOT EQUAL.
           IF WS-N4 = "12"
               DISPLAY "Q-3 EQ"
           ELSE
               DISPLAY "Q-3 NE".
      * Q-4: the same digits unquoted compare algebraically, EQUAL.
           IF WS-N4 = 12
               DISPLAY "Q-4 EQ"
           ELSE
               DISPLAY "Q-4 NE".
           STOP RUN.
