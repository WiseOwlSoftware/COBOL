*>--- conformance-case
*> id: pb4830-baseconvert-folded-length-unsigned-position
*> rules: [SR-8.4.3.2.3-12]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.4.3.2.3 SR12 (cite.py --check 8.4.3.2.3 "An integer function other than the integer form of the ABS
      *> function shall not be specified where an unsigned integer is required" -> OK): FUNCTION LENGTH and
      *> FUNCTION BYTE-LENGTH are integer functions (15.2 item 5) other than ABS, so a position that requires an unsigned
      *> integer (CONCAT 15.18.3 r3; BASECONVERT 15.12.3 r1 below base 11) bars them whatever tree the binder gives them.
      *> This one is a BYTE-LENGTH the binder FOLDS to a literal (a fixed item).
      *> IT COMPILED CLEAN (CONCAT printed the length followed by the string), kb/Work PB4830.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4830NEG3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(10).
       01 FX PIC X(3) VALUE "ABC".
       PROCEDURE DIVISION.
           MOVE FUNCTION BASECONVERT(FUNCTION BYTE-LENGTH(FX) 10 8) TO W
           STOP RUN.
