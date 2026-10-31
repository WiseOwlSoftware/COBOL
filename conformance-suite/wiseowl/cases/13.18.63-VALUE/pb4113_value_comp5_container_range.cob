*>--- conformance-case
*> id: pb4113_value_comp5_container_range
*> rules: [SR-13.18.63.3-2, DOC-A.1-208]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4113. ISO 1989:2023 13.18.63.3 SR2: "If the category of the subject of the entry is numeric, all literals in
*> the VALUE clause shall be numeric and shall be permissible values within the range indicated by the PICTURE clause or
*> the USAGE clause." COMP-5 is a WiseOwl COBOL extension (4.2.10) whose range comes from its USAGE: docs/CONFORMANCE.md
*> DOC-A.1-208 bounds the value by the native two's-complement range of the byte width, never by the PICTURE's digit
*> count, so a VALUE literal is admitted exactly where a MOVE, an arithmetic store and SET of an 88 admit it. The
*> container of a PICTURE is the smallest of 1, 2, 4, 8 and 16 bytes that holds its digits (DOC-A.1-205), so:
*>   PIC 9(4) COMP-5 (2 bytes, unsigned) holds 0 through 65535, and 65535 is admitted; PIC S9(4) COMP-5 holds -32768
*>   through 32767 (both edges are admitted); PIC 9(2) COMP-5 (1 byte) holds 255; PIC S99V99 COMP-5 holds -327.68
*>   through 327.67 (the container's band at the PICTURE's scale, two places); PIC PP99 COMP-5 (1 byte, scale 4) holds
*>   0 through .0255; PIC 9(9) COMP-5 (4 bytes) holds 4294967295 and PIC 9(18) COMP-5 (8 bytes) 18446744073709551615.
*> The level-88 entries ride the same screen (13.18.63.3 formats 2 and 3 share SR2): BIG VALUE 10000 THRU 65535 is true
*> for B1 = 40000, SET LIT TO FALSE stores the WHEN SET TO FALSE literal 65534 (format 3), and the table element value
*> (format 2) is stored in every occurrence. Every value is read back
*> through a comparison, which is edition-independent, and every line prints OK. COBOL-85 limits a literal to 18 digits
*> (the 20-digit literal of A9 needs 2002), so the test starts there. The refused neighbours, one past each edge, are pb4113-value-comp5-above-container-band and pb4113-value-comp5-trailing-digits.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4113RUN.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 A1 PIC 9(4) COMP-5 VALUE 65535.
01 A2 PIC S9(4) COMP-5 VALUE -32768.
01 A3 PIC S9(4) COMP-5 VALUE 32767.
01 A4 PIC 9(2) COMP-5 VALUE 255.
01 A5 PIC S99V99 COMP-5 VALUE -327.68.
01 A6 PIC S99V99 COMP-5 VALUE 327.67.
01 A7 PIC PP99 COMP-5 VALUE .0255.
01 A8 PIC 9(9) COMP-5 VALUE 4294967295.
01 A9 PIC 9(18) COMP-5 VALUE 18446744073709551615.
01 B1 PIC 9(4) COMP-5 VALUE 40000.
   88 BIG VALUE 10000 THRU 65535.
   88 PEAK VALUE 65535.
01 B2 PIC 9(4) COMP-5 VALUE 1.
   88 LIT VALUE 65535 WHEN SET TO FALSE 65534.
01 T1.
   05 E1 PIC 9(4) COMP-5 OCCURS 3 VALUE 65000.
PROCEDURE DIVISION.
MAIN-PARA.
    IF A1 = 65535 DISPLAY "A1 OK" END-IF
    IF A2 = -32768 DISPLAY "A2 OK" END-IF
    IF A3 = 32767 DISPLAY "A3 OK" END-IF
    IF A4 = 255 DISPLAY "A4 OK" END-IF
    IF A5 = -327.68 DISPLAY "A5 OK" END-IF
    IF A6 = 327.67 DISPLAY "A6 OK" END-IF
    IF A7 = .0255 DISPLAY "A7 OK" END-IF
    IF A8 = 4294967295 DISPLAY "A8 OK" END-IF
    IF A9 = 18446744073709551615 DISPLAY "A9 OK" END-IF
    IF BIG DISPLAY "BIG OK" END-IF
    SET PEAK TO TRUE
    IF B1 = 65535 DISPLAY "PEAK OK" END-IF
    SET LIT TO FALSE
    IF B2 = 65534 DISPLAY "FALSE OK" END-IF
    IF E1 (1) = 65000 AND E1 (2) = 65000 AND E1 (3) = 65000 DISPLAY "E1 OK" END-IF
    STOP RUN.
