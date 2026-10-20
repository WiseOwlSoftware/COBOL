*>--- conformance-case
*> id: pb3441_vlg_display_national_members
*> rules: [GR-14.9.11.4-7, DOC-A.1-57, DOC-A.1-63]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3441 - DISPLAY, FUNCTION LENGTH, FUNCTION BYTE-LENGTH, MOVE and comparison of a VARIABLE-LENGTH GROUP
      *> that holds a DYNAMIC-LENGTH NATIONAL item and a DYNAMIC-CAPACITY TABLE of national elements.
      *>
      *> ISO 1989:2023 14.9.11.4 GR7: "If identifier-1 references a variable-length group, the format in which its
      *> contents are displayed is defined by the implementor." DOC-A.1-57 is that determination: the members' images
      *> in declaration order, each dynamic member at its CURRENT extent, and the image of a group IS its storage, so a
      *> NATIONAL member - fixed or dynamic-length alike - contributes the bytes it occupies: two a character position,
      *> UTF-16BE, high-order byte first (13.18.60.4 GR8 leaves the size to the implementor; D-N1 pins two). 15.50.4
      *> rule 7 makes FUNCTION LENGTH of the group the sum of its members' extents, so the displayed width, LENGTH and
      *> BYTE-LENGTH are one number. Before the fix a dynamic national leaf and a national table element were
      *> displayed ONE character a position, so DISPLAY showed a width LENGTH did not count, and a MOVE of such a
      *> group into another stored the character image as though it were the table's byte image (a corrupted table).
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE DETERMINATION - NOT FROM A RUN. The national literals are hexadecimal so
      *> that each position's two bytes are printable ASCII: NX"4142" is ONE position, U+4142, whose bytes are 41 42,
      *> "AB". Each table element is N(2), four bytes.
      *>   G1  GD "ABCD" (2 positions, 4 bytes) + GT(1) "EFGH" + GT(2) "IJKL" + GN "06"        4 + 8 + 2 = 14
      *>   G2  GD shortened to one position "MN" (the table and GN are unchanged)               2 + 8 + 2 = 12
      *>   H   MOVE G TO H gives H the same members at the same extents (14.9.25.4 GR9; 14.6.9.2)   12
      *>   Q   QH "h" + QA "AB" + QB "7" + QT(1) "CD" "x" + QT(2) "EFGH" "y" - the nested group and a FIXED table
      *>       of variable-length elements, each occurrence its own national item                 1 + 2 + 1 + 3 + 5 = 12
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3441A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 GD PIC N DYNAMIC LENGTH LIMIT IS 10.
          05 GT PIC N(2) OCCURS DYNAMIC CAPACITY IN GC FROM 1.
          05 GN PIC 9(2).
       01 H.
          05 HD PIC N DYNAMIC LENGTH LIMIT IS 10.
          05 HT PIC N(2) OCCURS DYNAMIC CAPACITY IN HC FROM 1.
          05 HN PIC 9(2).
       01 Q.
          05 QH PIC X VALUE "h".
          05 QI.
             10 QA PIC N DYNAMIC LENGTH LIMIT IS 6.
             10 QB PIC 9.
          05 QT OCCURS 2.
             10 QN PIC N DYNAMIC LENGTH LIMIT IS 4.
             10 QX PIC X.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE NX"41424344" TO GD
           MOVE NX"45464748" TO GT(1)
           MOVE NX"494A4B4C" TO GT(2)
           MOVE 6 TO GN
           DISPLAY "G1=[" G "] " FUNCTION LENGTH (G) " " FUNCTION BYTE-LENGTH (G)
           MOVE NX"4D4E" TO GD
           DISPLAY "G2=[" G "] " FUNCTION LENGTH (G) " " FUNCTION BYTE-LENGTH (G)
           MOVE G TO H
           DISPLAY "H=[" H "] " FUNCTION LENGTH (H) " " FUNCTION BYTE-LENGTH (H)
           IF G = H DISPLAY "EQ" ELSE DISPLAY "NE" END-IF
           IF HD = NX"4D4E" DISPLAY "HD-OK" ELSE DISPLAY "HD-BAD" END-IF
           IF HT (2) = NX"494A4B4C" DISPLAY "HT2-OK" ELSE DISPLAY "HT2-BAD" END-IF
           MOVE NX"4142" TO QA
           MOVE 7 TO QB
           MOVE NX"4344" TO QN (1)
           MOVE "x" TO QX (1)
           MOVE NX"45464748" TO QN (2)
           MOVE "y" TO QX (2)
           DISPLAY "Q=[" Q "] " FUNCTION LENGTH (Q) " " FUNCTION BYTE-LENGTH (Q)
           STOP RUN.
       END PROGRAM PB3441A.
