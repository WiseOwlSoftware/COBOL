*>--- conformance-case
*> id: pb4830-concat-variable-group-length-unsigned-position
*> rules: [SR-8.4.3.2.3-12]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.4.3.2.3 SR12 (cite.py --check 8.4.3.2.3 "An integer function other than the integer form of the ABS
      *> function shall not be specified where an unsigned integer is required" -> OK): FUNCTION LENGTH and
      *> FUNCTION BYTE-LENGTH are integer functions (15.2 item 5) other than ABS, so a position that requires an unsigned
      *> integer (CONCAT 15.18.3 r3; BASECONVERT 15.12.3 r1 below base 11) bars them whatever tree the binder gives them.
      *> This one is the run-time LENGTH of a variable-length group (an unfolded sum).
      *> IT COMPILED CLEAN (CONCAT printed the length followed by the string), kb/Work PB4830.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4830NEG2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(10).
       01 N PIC 9 VALUE 2.
       01 VG.
          05 T PIC X OCCURS 1 TO 5 DEPENDING ON N.
       PROCEDURE DIVISION.
           MOVE FUNCTION CONCAT(FUNCTION LENGTH(VG) "A") TO W
           STOP RUN.
