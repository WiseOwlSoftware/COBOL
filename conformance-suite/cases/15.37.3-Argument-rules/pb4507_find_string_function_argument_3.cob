*>--- conformance-case
*> id: pb4507_find_string_function_argument_3
*> rules: [AR-15.37.3-3, RV-15.37.4-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB4507 - ISO §2023:15.37.3 r3: "argument-3 shall be an integer data item or integer literal." (cite.py --check
      *> 15.37.3 OK). §2023:8.4.3.2.4 r1: "A function-identifier references a temporary data item whose value is determined
      *> when the function is referenced at runtime." An INTEGER function (§2023:15.2 item 5: "An integer function has an
      *> operational sign and no digits to the right of the decimal point") therefore returns an integer data item, and
      *> argument-3 may be one. The screen used to bar every nested function; an arithmetic expression stays barred
      *> (negative/pb58-findstr-expr), and so does a numeric function that is not an integer function, by the §2023:15.3
      *> type 6 integer test (negative/pb4507-findstr-numeric-function).
      *>
      *> EXPECTED VALUES, from §2023:15.37.4 r2 - "argument-3 represents the number of matches to ignore before determining the
      *> character position that shall be returned": "ABC" occurs in "ABCABCABC" at positions 1, 4 and 7, so ignoring 1
      *> match returns 4, ignoring 2 returns 7, and ignoring 3 (no match left, r3) returns 0. FUNCTION LENGTH of the
      *> variable-length group VG (an integer function, §2023:15.50.1) is the integer 2 with NV = 2, so it ignores 2 matches
      *> and returns 7 (review R1-1: the binder lowers it to a sum, but it is still a function-identifier).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4507FINDSTR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X9   PIC X(9) VALUE "ABCABCABC".
       01 I2   PIC 9 VALUE 2.
       01 NV   PIC 9 VALUE 2.
       01 VG.
          05 VE PIC X OCCURS 1 TO 9 DEPENDING ON NV.
       01 T    PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE T = FUNCTION FIND-STRING(X9 "ABC" FUNCTION INTEGER(1))
           DISPLAY "INTEGER1=" T
           COMPUTE T = FUNCTION FIND-STRING(X9 "ABC" FUNCTION ABS(I2))
           DISPLAY "ABSITEM=" T
           COMPUTE T = FUNCTION FIND-STRING(X9 "ABC" FUNCTION INTEGER(3))
           DISPLAY "NOMATCH=" T
           COMPUTE T = FUNCTION FIND-STRING(X9 "ABC" FUNCTION LENGTH(VG))
           DISPLAY "LENGTHVG=" T
           STOP RUN.
       END PROGRAM PB4507FINDSTR.
