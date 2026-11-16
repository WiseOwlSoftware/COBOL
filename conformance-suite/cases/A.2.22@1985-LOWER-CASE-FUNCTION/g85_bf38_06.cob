      *>--- conformance-case
      *> id: g85_bf38_06
      *> rules: [1989-RV-A.2.22.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.22.4 RV1 - the same character string is returned with
      *> each uppercase letter replaced by its lowercase letter: lowercase
      *> letters, digits, spaces and punctuation are unchanged, for an
      *> alphanumeric item, an alphabetic item and a nonnumeric literal.
      *> A letter left uppercase, or any other character changed, fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3806.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  SRC         PIC X(16)   VALUE "HELLO World 12!z".
       01  ALF         PIC A(7)    VALUE "ABC DEF".
       01  T16         PIC X(16).
       01  T7          PIC X(7).
       01  T5          PIC X(5).
       PROCEDURE DIVISION.
       P-MAIN.
           MOVE FUNCTION LOWER-CASE (SRC) TO T16.
           DISPLAY "[" T16 "]".
           MOVE FUNCTION LOWER-CASE (ALF) TO T7.
           DISPLAY "[" T7 "]".
           MOVE FUNCTION LOWER-CASE ("MiXeD") TO T5.
           DISPLAY "[" T5 "]".
           IF FUNCTION LOWER-CASE (SRC) = "hello world 12!z"
               DISPLAY "COMPARE EQUAL"
           ELSE
               DISPLAY "COMPARE UNEQUAL".
           STOP RUN.
