*>--- conformance-case
*> id: pb4635-length-parenthesized-boolean-item
*> rules: [AR-15.50.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> PB4635 review R3-3 - ISO §15.50.3 r1: "Argument-1 shall be an alphanumeric, national, or boolean literal; a data item of any
      *> class or category; a based entry; or a type-name." (BB) is "a boolean expression enclosed in parentheses" (§8.8.2), none of
      *> those, exactly as (I2) is an arithmetic expression (§8.8.1.1) and LENGTH((I2)) is refused: the argument binder returned the
      *> boolean one as the item BB, so LENGTH((BB)) printed 4.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGLENGTHPARENBOOL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BB    PIC 1(4) VALUE B"1010".
       01 N     PIC 9(4).
       PROCEDURE DIVISION.
           COMPUTE N = FUNCTION LENGTH((BB)).
           STOP RUN.
