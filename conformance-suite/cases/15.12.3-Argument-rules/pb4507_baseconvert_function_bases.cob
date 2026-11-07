*>--- conformance-case
*> id: pb4507_baseconvert_function_bases
*> rules: [AR-15.12.3-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB4507 - ISO §2023:15.12.3 r1: "Argument-2 and argument-3 shall be positive nonzero numeric integer literals or data
      *> items with unequal values in the range 2 to 16." (cite.py --check 15.12.3 OK). §2023:8.4.3.2.4 r1: "A function-identifier
      *> references a temporary data item whose value is determined when the function is referenced at runtime." An integer
      *> function (§2023:15.2 item 5) returns an integer data item, so a base may be one; an arithmetic expression stays barred
      *> (negative/pb2079-baseconvert-expression-base) and so does a numeric function that is not an integer function.
      *> The screen used to bar every nested function.
      *>
      *> EXPECTED VALUES: "1A" in base 16 is 1 * 16 + 10 = 26 (§2023:15.12.4 r1: the value of the digits expressed in the base of
      *> argument-3). FUNCTION INTEGER(16) and FUNCTION INTEGER(10) are the integers 16 and 10 (§2023:15.44), FUNCTION ABS(I16)
      *> is the integer 16 of an integer item, so each spelling below returns "26". FUNCTION LENGTH (§2023:15.50.1: "The type of
      *> this function is integer") of the variable-length group VG is the integer function it is whatever tree the binder
      *> lowers it to (a sum of the table's occurrences): with NV = 16 the group is 16 characters long, so the fourth
      *> spelling also returns "26" (review R1-1, the binder's statement and not the tree's shape decides).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4507BASECONV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 I16  PIC 99 VALUE 16.
       01 NV   PIC 99 VALUE 16.
       01 VG.
          05 VE PIC X OCCURS 1 TO 20 DEPENDING ON NV.
       01 R    PIC X(40).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION BASECONVERT("1A" FUNCTION INTEGER(16) 10) TO R
           DISPLAY "FROM=" R
           MOVE FUNCTION BASECONVERT("1A" 16 FUNCTION INTEGER(10)) TO R
           DISPLAY "TO=" R
           MOVE FUNCTION BASECONVERT("1A" FUNCTION ABS(I16) FUNCTION INTEGER(10)) TO R
           DISPLAY "BOTH=" R
           MOVE FUNCTION BASECONVERT("1A" FUNCTION LENGTH(VG) 10) TO R
           DISPLAY "LENGTHVG=" R
           STOP RUN.
       END PROGRAM PB4507BASECONV.
