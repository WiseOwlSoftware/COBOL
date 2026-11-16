      *>--- conformance-case
      *> id: g85_f13_05
      *> rules: [1989-GR-A.2.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.3 GR1 - an alphanumeric function is of class and
      *> category alphanumeric, its size is the one its definition
      *> states, and its usage is implicitly DISPLAY.
      *>   LEN   size: REVERSE returns exactly the length of argument-1
      *>         (§1989:A.2.38.1), so LENGTH of it is 5.
      *>   [..]  category alphanumeric: an alphanumeric to alphanumeric
      *>         move, left aligned and space filled to 8; and to an
      *>         alphanumeric edited receiver (XXBXXX).
      *>   NUM   category alphanumeric: to a numeric receiver the value
      *>         moves as an unsigned integer (§1985:VI.6.19.4 GR4b3).
      *>   CLS   class conditions on the function: admitted only for an
      *>         operand whose usage is DISPLAY (§1985:VI.6.3.1.2).
      *> Why each leg can fail: a refusal of the class condition denies
      *> the implicit DISPLAY usage; a wrong length, alignment or value
      *> denies the size or the category.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F1305.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W5      PIC X(5) VALUE "ab1cd".
       01  W2      PIC X(2) VALUE "21".
       01  W3      PIC X(3) VALUE "xyz".
       01  N4      PIC 9(4).
       01  R-X8    PIC X(8).
       01  R-ANED  PIC XXBXXX.
       01  R-N4    PIC 9(4).
       PROCEDURE DIVISION.
       P1.
           COMPUTE N4 = FUNCTION LENGTH (FUNCTION REVERSE (W5)).
           DISPLAY "LEN=" N4.
           MOVE FUNCTION UPPER-CASE (W5) TO R-X8.
           DISPLAY "[" R-X8 "]".
           MOVE FUNCTION REVERSE (W5) TO R-ANED.
           DISPLAY "[" R-ANED "]".
           MOVE FUNCTION REVERSE (W2) TO R-N4.
           DISPLAY "NUM=" R-N4.
           IF FUNCTION UPPER-CASE (W3) IS ALPHABETIC
               DISPLAY "CLS1=ALPHABETIC"
           ELSE
               DISPLAY "CLS1=NOT-ALPHABETIC".
           IF FUNCTION REVERSE (W2) IS NUMERIC
               DISPLAY "CLS2=NUMERIC"
           ELSE
               DISPLAY "CLS2=NOT-NUMERIC".
           STOP RUN.
