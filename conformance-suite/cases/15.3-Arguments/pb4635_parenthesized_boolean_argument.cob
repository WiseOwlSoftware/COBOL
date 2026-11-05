*>--- conformance-case
*> id: pb4635_parenthesized_boolean_argument
*> rules: [AR-15.3-3, AR-15.45.3-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4635 review R2-1. ISO §8.4.3.2.3 SR8: "Argument-1 shall be an identifier, a literal, a boolean expression, or an
      *> arithmetic expression" (cite.py --check OK), and §8.8.2 lists "a boolean expression enclosed in parentheses" among the
      *> boolean expressions (cite.py --check OK). So a class-boolean item written in grouping parentheses is a boolean expression, and
      *> §15.45.3 r1 ("Argument-1 shall be of class boolean.", cite.py --check OK) admits it exactly as it admits the bare item. The
      *> same written shape is also an arithmetic expression (§8.8.1.1), whose operands must be numeric, which is why a parenthesized
      *> ALPHANUMERIC item is refused (pb4635-abs-parenthesized-alphanumeric) while a parenthesized BOOLEAN one is not.
      *>
      *> EXPECTED VALUES are the bit strings read as unsigned binary integers (§15.45.4): BB = B"1010" is 10; BT = B"0110" is 6; the
      *> first occurrence of BE = B"0011" is 3; BOOLEAN-OF-INTEGER(2 4) is the 4-bit string 0010, 2; B"1001" is 9.
      *> CONCAT (review R3-3): §15.3 type 3 ("Boolean. A bit group item, a boolean expression or literal, or an elementary boolean data
      *> item", cite.py --check OK) and §15.18.3 r1's class boolean admit the enclosed boolean expression (the bare item is the same
      *> value), so CONCAT((BB) "A") is the characters of BB followed by "A" (§15.18.4 r1): 1010A. The position where it is NOT legal,
      *> LENGTH, is the refusal pb4635-length-parenthesized-boolean-item.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4635PARENBOOL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BB PIC 1(4) VALUE B"1010".
       01 BT PIC 1(4) USAGE BIT VALUE B"0110".
       01 BTAB.
          05 BE PIC 1(4) OCCURS 2 VALUE B"0011".
       01 R PIC 9(6).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION INTEGER-OF-BOOLEAN(BB)
           DISPLAY "BARE=" R
           COMPUTE R = FUNCTION INTEGER-OF-BOOLEAN((BB))
           DISPLAY "ITEM=" R
           COMPUTE R = FUNCTION INTEGER-OF-BOOLEAN(((BB)))
           DISPLAY "NESTED=" R
           COMPUTE R = FUNCTION INTEGER-OF-BOOLEAN((BT))
           DISPLAY "BIT=" R
           COMPUTE R = FUNCTION INTEGER-OF-BOOLEAN((BE(1)))
           DISPLAY "SUBSCRIPTED=" R
           COMPUTE R = FUNCTION INTEGER-OF-BOOLEAN
               ((FUNCTION BOOLEAN-OF-INTEGER(2 4)))
           DISPLAY "FUNCTION=" R
           COMPUTE R = FUNCTION INTEGER-OF-BOOLEAN((B"1001"))
           DISPLAY "LITERAL=" R
           COMPUTE R = FUNCTION INTEGER-OF-BOOLEAN((BB) B-AND BB)
           DISPLAY "AND=" R
           DISPLAY "CONCAT=" FUNCTION CONCAT((BB) "A")
           STOP RUN.
       END PROGRAM PB4635PARENBOOL.
